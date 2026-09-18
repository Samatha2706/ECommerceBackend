using ClosedXML.Excel;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Application.Services;

public class BulkProductUploadService : IBulkProductUploadService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IGenericRepository<Inventory> _inventoryRepository;
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public BulkProductUploadService(
        IGenericRepository<Product> productRepository,
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Inventory> inventoryRepository,
        IApplicationDbContext context,
        IMemoryCache cache)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _inventoryRepository = inventoryRepository;
        _context = context;
        _cache = cache;
    }
    public async Task<BulkProductUploadResultDto> UploadAsync(
    Stream fileStream)
    {
        const int batchSize = 3;

        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            throw new InvalidOperationException(
                "The Excel file does not contain a worksheet.");
        }
        var expectedHeaders = new[]
        {
            "SKU",
            "Name",
            "Description",
            "Price",
            "CategoryId",
            "InitialQuantity",
            "ReorderLevel"
        };

        for (int column = 1; column <= expectedHeaders.Length; column++)
        {
            var actualHeader = worksheet.Cell(1, column)
                .GetString()
                .Trim();

            if (!actualHeader.Equals(
                    expectedHeaders[column - 1],
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Invalid Excel header in column {column}. " +
                    $"Expected '{expectedHeaders[column - 1]}'.");
            }
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        if (lastRow < 2)
        {
            throw new InvalidOperationException(
                "The Excel file does not contain any product rows.");
        }

        var result = new BulkProductUploadResultDto
        {
            TotalRecords = lastRow - 1,
            BatchSize = batchSize,
        };

        var newProducts = new List<Product>();
        var newInventories = new List<Inventory>();

        var updates = new List<(Product Product, int CategoryId, string Name, string
            ? Description, decimal Price, int Quantity, int ReorderLevel)
            >();

        var errors = new List<string>();

        var existingProducts = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();

        var existingProductsBySku = existingProducts
    .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
    .ToDictionary(
        p => p.SKU,
        StringComparer.OrdinalIgnoreCase);

        var excelSkus = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var nextSkuNumber = existingProducts
            .Select(p => p.SKU)
            .Where(sku => sku.StartsWith("PROD-"))
            .Select(sku =>
                int.TryParse(sku.Substring(5), out var number)
                    ? number
                    : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;


        for (int row = 2; row <= lastRow; row++)
        {
            var sku = worksheet.Cell(row, 1)
                .GetString()
                .Trim();

            var name = worksheet.Cell(row, 2)
                .GetString()
                .Trim();

            var description = worksheet.Cell(row, 3)
                .GetString()
                .Trim();

            var priceCell = worksheet.Cell(row, 4);
            var categoryCell = worksheet.Cell(row, 5);
            var quantityCell = worksheet.Cell(row, 6);
            var reorderLevelCell = worksheet.Cell(row, 7);

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Row {row}: Product name is required.");
                continue;
            }

            if (!priceCell.TryGetValue<decimal>(out var price) ||
                price <= 0)
            {
                errors.Add(
                    $"Row {row}: Price must be a valid value greater than 0.");
                continue;
            }

            if (!categoryCell.TryGetValue<int>(out var categoryId))
            {
                errors.Add(
                    $"Row {row}: CategoryId must be a valid integer.");
                continue;
            }

            if (!quantityCell.TryGetValue<int>(out var initialQuantity) ||
                initialQuantity < 0)
            {
                errors.Add(
                    $"Row {row}: InitialQuantity must be a valid integer " +
                    "greater than or equal to 0.");
                continue;
            }

            if (!reorderLevelCell.TryGetValue<int>(out var reorderLevel) ||
                reorderLevel < 0)
            {
                errors.Add(
                    $"Row {row}: ReorderLevel must be a valid integer " +
                    "greater than or equal to 0.");
                continue;
            }

            var categoryExists = categories.Any(
                category => category.Id == categoryId);

            if (!categoryExists)
            {
                errors.Add(
                    $"Row {row}: CategoryId {categoryId} does not exist.");
                continue;
            }

            // If SKU is provided, check whether the product already exists
            if (!string.IsNullOrWhiteSpace(sku))
            {
                if (!excelSkus.Add(sku))
                {
                    errors.Add(
                        $"Row {row}: Duplicate SKU '{sku}' in Excel file.");
                    continue;
                }

                if (existingProductsBySku.TryGetValue(sku, out var existingProduct))
                {
                    updates.Add((
                        existingProduct,
                        categoryId,
                        name,
                        string.IsNullOrWhiteSpace(description)
                            ? null
                            : description,
                        price,
                        initialQuantity,
                        reorderLevel
                    ));

                    continue;
                }
            }

            // Blank SKU OR new SKU → create new product
            var generatedSku = string.IsNullOrWhiteSpace(sku)
                ? $"PROD-{nextSkuNumber++:D3}"
                : sku;

            var product = new Product
            {
                SKU = generatedSku,
                Name = name,
                Description = string.IsNullOrWhiteSpace(description)
                    ? null
                    : description,
                Price = price,
                CategoryId = categoryId,
                IsActive = true
            };

            newProducts.Add(product);

            var inventory = new Inventory
            {
                Product = product,
                Quantity = initialQuantity,
                ReorderLevel = reorderLevel
            };

            newInventories.Add(inventory);

        }

        result.FailedRecords = errors.Count;
        result.Errors = errors;

        if (newProducts.Count == 0 &&
    updates.Count == 0)
        {
            return result;
        }

        var successfulRecords = 0;
        var batchesProcessed = 0;

        // Process existing products in batches
        for (int i = 0; i < updates.Count; i += batchSize)
        {
            var updateBatch = updates
                .Skip(i)
                .Take(batchSize)
                .ToList();

            var batchNumber = batchesProcessed + 1;

            var executionStrategy = _context.Database
                .CreateExecutionStrategy();

            try
            {
                await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction =
                        await _context.Database.BeginTransactionAsync();

                    try
                    {
                        foreach (var update in updateBatch)
                        {
                            var product = update.Product;

                            product.Name = update.Name;
                            product.Description = update.Description;
                            product.Price = update.Price;
                            product.CategoryId = update.CategoryId;

                            var inventory = await _context.Inventories
                                .FirstOrDefaultAsync(i =>
                                    i.ProductId == product.Id);

                            if (inventory is not null)
                            {
                                inventory.Quantity = update.Quantity;
                                inventory.ReorderLevel = update.ReorderLevel;
                            }
                        }

                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                successfulRecords += updateBatch.Count;
                batchesProcessed++;
            }
            catch (Exception ex)
            {
                errors.Add(
                    $"Batch {batchNumber} failed: {ex.Message}");
            }
        }

        result.SuccessfulRecords = successfulRecords;
        result.BatchesProcessed = batchesProcessed;
        result.FailedRecords = result.TotalRecords - result.SuccessfulRecords;
        result.Errors = errors;

        _cache.Remove("products_all");

        return result;
    }
    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Products");

        worksheet.Cell(1, 1).Value = "SKU";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Description";
        worksheet.Cell(1, 4).Value = "Price";
        worksheet.Cell(1, 5).Value = "CategoryId";
        worksheet.Cell(1, 6).Value = "InitialQuantity";
        worksheet.Cell(1, 7).Value = "ReorderLevel";

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();

    }

}