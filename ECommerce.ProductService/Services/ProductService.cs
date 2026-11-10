using ECommerce.ProductService.Data;
using ECommerce.ProductService.DTOs.Products;
using ECommerce.ProductService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ClosedXML.Excel;

namespace ECommerce.ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly IMemoryCache _cache;

    public ProductService(
        ProductDbContext context,
        IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ProductPagedResultDto> GetAllAsync(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        string? sortOrder = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            products = products
                .Where(product =>
                    product.Name.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    (product.Description != null &&
                     product.Description.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (categoryId.HasValue)
        {
            products = products
                .Where(p => p.CategoryId == categoryId.Value)
                .ToList();
        }

        if (minPrice.HasValue)
        {
            products = products
                .Where(p => p.Price >= minPrice.Value)
                .ToList();
        }

        if (maxPrice.HasValue)
        {
            products = products
                .Where(p => p.Price <= maxPrice.Value)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            sortBy = sortBy.Trim().ToLowerInvariant();
            sortOrder = sortOrder?.Trim().ToLowerInvariant();

            var descending = sortOrder == "desc";

            products = sortBy switch
            {
                "price" => descending
                    ? products.OrderByDescending(p => p.Price).ToList()
                    : products.OrderBy(p => p.Price).ToList(),

                "name" => descending
                    ? products.OrderByDescending(p => p.Name).ToList()
                    : products.OrderBy(p => p.Name).ToList(),

                _ => products
            };
        }

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        var totalCount = products.Count;

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        var pagedProducts = products
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = pagedProducts
            .Select(product =>
                MapToDto(
                    product,
                    product.Category,
                    product.Inventory))
            .ToList();

        return new ProductPagedResultDto
        {
            Products = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return null;
        }

        return MapToDto(
            product,
            product.Category,
            product.Inventory);
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductDto createProductDto)
    {
        var name = createProductDto.Name.Trim();

        var category = await _context.Categories
            .FirstOrDefaultAsync(
                c => c.Id == createProductDto.CategoryId);

        if (category is null)
        {
            throw new InvalidOperationException(
                "The specified category does not exist.");
        }

        var products = await _context.Products
            .ToListAsync();

        // Generate the next SKU
        var nextNumber = products
            .Select(p => p.SKU)
            .Where(sku => sku.StartsWith("PROD-"))
            .Select(sku =>
                int.TryParse(
                    sku.Substring(5),
                    out var number)
                    ? number
                    : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var sku = $"PROD-{nextNumber:D3}";


        var product = new Product
        {
            SKU = sku,
            Name = name,
            Description = createProductDto.Description?.Trim(),
            Price = createProductDto.Price,
            CategoryId = createProductDto.CategoryId,
            IsActive = true
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        var inventory = new Inventory
        {
            ProductId = product.Id,
            Quantity = createProductDto.InitialQuantity,
            ReorderLevel = createProductDto.ReorderLevel
        };

        _context.Inventories.Add(inventory);

        await _context.SaveChangesAsync();

        _cache.Remove("products_all");

        return MapToDto(
            product,
            category,
            inventory);
    }

    public async Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductDto updateProductDto)
    {
        var product = await _context.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return null;
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(
                c => c.Id == updateProductDto.CategoryId);

        if (category is null)
        {
            throw new InvalidOperationException(
                "The specified category does not exist.");
        }

        var name = updateProductDto.Name.Trim();

        var productNameExists = await _context.Products
            .AnyAsync(existingProduct =>
                existingProduct.Id != id &&
                existingProduct.Name.ToLower() == name.ToLower());

        if (productNameExists)
        {
            throw new InvalidOperationException(
                "A product with this name already exists.");
        }

        product.Name = name;
        product.Description =
            updateProductDto.Description?.Trim();
        product.Price = updateProductDto.Price;
        product.CategoryId = updateProductDto.CategoryId;
        product.IsActive = updateProductDto.IsActive;

        await _context.SaveChangesAsync();

        _cache.Remove("products_all");

        return MapToDto(
            product,
            category,
            product.Inventory);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return false;
        }

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();

        _cache.Remove("products_all");

        return true;
    }

    public async Task<BulkProductUploadResultDto> UploadBulkAsync(
    Stream fileStream)
    {
        const int batchSize = 3;

        var result = new BulkProductUploadResultDto
        {
            BatchSize = batchSize
        };

        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheet(1);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        if (lastRow < 2)
        {
            throw new InvalidOperationException(
                "The Excel file contains no product records.");
        }

        // Validate the Excel headers
        string[] requiredHeaders =
        {
        "SKU",
        "Name",
        "Description",
        "Price",
        "CategoryId",
        "InitialQuantity",
        "ReorderLevel"
    };

        for (int i = 0; i < requiredHeaders.Length; i++)
        {
            var actualHeader = worksheet.Cell(1, i + 1)
                .GetString()
                .Trim();


            if (!actualHeader.Equals(
                requiredHeaders[i],
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Invalid Excel column {i + 1}. Expected '{requiredHeaders[i]}'.");
            }
        }

        result.TotalRecords = lastRow - 1;

        var existingSkus = await _context.Products
            .Select(p => p.SKU)
            .ToListAsync();

        var usedSkus = new HashSet<string>(
            existingSkus,
            StringComparer.OrdinalIgnoreCase);

        var nextSkuNumber = existingSkus
            .Where(s => s.StartsWith(
                "PROD-",
                StringComparison.OrdinalIgnoreCase))
            .Select(s => int.TryParse(s.Substring(5), out var number)
                ? number
                : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var uploadedSkus = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        for (int rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            try
            {
                var sku = worksheet.Cell(rowNumber, 1)
                    .GetString().Trim();

                var isGeneratedSku = string.IsNullOrWhiteSpace(sku);

                var name = worksheet.Cell(rowNumber, 2)
                    .GetString().Trim();

                var description = worksheet.Cell(rowNumber, 3)
                    .GetString().Trim();

                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidOperationException("Product name is required.");

                if (!worksheet.Cell(rowNumber, 4)
                    .TryGetValue<decimal>(out var price) || price <= 0)
                    throw new InvalidOperationException("Invalid product price.");

                if (!worksheet.Cell(rowNumber, 5)
                    .TryGetValue<int>(out var categoryId))
                    throw new InvalidOperationException("Invalid CategoryId.");

                if (!worksheet.Cell(rowNumber, 6)
                    .TryGetValue<int>(out var quantity) || quantity < 0)
                    throw new InvalidOperationException("Invalid InitialQuantity.");

                if (!worksheet.Cell(rowNumber, 7)
                    .TryGetValue<int>(out var reorderLevel) || reorderLevel < 0)
                    throw new InvalidOperationException("Invalid ReorderLevel.");

                if (!string.IsNullOrWhiteSpace(sku) &&
                    !uploadedSkus.Add(sku))
                {
                    throw new InvalidOperationException(
                        $"Duplicate SKU '{sku}' in the uploaded file.");
                }

                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == categoryId);

                if (!categoryExists)
                {
                    throw new InvalidOperationException(
                        $"CategoryId {categoryId} does not exist.");
                }

                // Generate an SKU if the Excel cell is blank
                if (string.IsNullOrWhiteSpace(sku))
                {
                    do
                    {
                        sku = $"PROD-{nextSkuNumber:D3}";
                        nextSkuNumber++;
                    }
                    while (!usedSkus.Add(sku));
                }

                var product = await _context.Products
                    .Include(p => p.Inventory)
                    .FirstOrDefaultAsync(p => p.SKU == sku);

                if (product != null)
                {
                    // Update an existing product
                    product.Name = name;
                    product.Description = description;
                    product.Price = price;
                    product.CategoryId = categoryId;

                    if (product.Inventory == null)
                    {
                        product.Inventory = new Inventory
                        {
                            ProductId = product.Id,
                            Quantity = quantity,
                            ReorderLevel = reorderLevel
                        };

                        _context.Inventories.Add(product.Inventory);
                    }
                    else
                    {
                        product.Inventory.Quantity = quantity;
                        product.Inventory.ReorderLevel = reorderLevel;
                    }
                }
                else
                {
                    // Create a new product
                    if (!isGeneratedSku && !usedSkus.Add(sku))
                    {
                        throw new InvalidOperationException(
                            $"SKU '{sku}' already exists.");
                    }

                    var newProduct = new Product
                    {
                        SKU = sku,
                        Name = name,
                        Description = description,
                        Price = price,
                        CategoryId = categoryId,
                        IsActive = true
                    };

                    _context.Products.Add(newProduct);

                    await _context.SaveChangesAsync();

                    var inventory = new Inventory
                    {
                        ProductId = newProduct.Id,
                        Quantity = quantity,
                        ReorderLevel = reorderLevel
                    };

                    _context.Inventories.Add(inventory);
                }

                await _context.SaveChangesAsync();

                result.SuccessfulRecords++;
            }
            catch (Exception ex)
            {
                result.FailedRecords++;

                result.Errors.Add(
                    $"Row {rowNumber}: {ex.Message}");
            }

            if ((rowNumber - 1) % batchSize == 0 ||
                rowNumber == lastRow)
            {
                result.BatchesProcessed++;
            }
        }

        _cache.Remove("products_all");

        return result;
    }

    public byte[] GenerateBulkTemplate()
    {
        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Products");

        string[] headers =
        {
        "SKU",
        "Name",
        "Description",
        "Price",
        "CategoryId",
        "InitialQuantity",
        "ReorderLevel"
    };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static ProductDto MapToDto(
        Product product,
        Category? category,
        Inventory? inventory)
    {
        return new ProductDto
        {
            Id = product.Id,
            SKU = product.SKU,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            AvailableQuantity = inventory?.Quantity ?? 0,
            ReorderLevel = inventory?.ReorderLevel ?? 0
        };
    }
}