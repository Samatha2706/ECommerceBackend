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
        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            throw new InvalidOperationException(
                "The Excel file does not contain a worksheet.");
        }
        var expectedHeaders = new[]
{
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
            TotalRows = lastRow - 1
        };

        var products = new List<Product>();
        var inventories = new List<Inventory>();
        var errors = new List<string>();

        var existingProducts = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();

        var existingProductNames = existingProducts
            .Select(p => p.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var excelProductNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        for (int row = 2; row <= lastRow; row++)
        {
            var name = worksheet.Cell(row, 1)
                .GetString()
                .Trim();

            var description = worksheet.Cell(row, 2)
                .GetString()
                .Trim();

            var priceCell = worksheet.Cell(row, 3);
            var categoryCell = worksheet.Cell(row, 4);
            var quantityCell = worksheet.Cell(row, 5);
            var reorderLevelCell = worksheet.Cell(row, 6);

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

            if (existingProductNames.Contains(name))
            {
                errors.Add(
                    $"Row {row}: A product named '{name}' already exists.");
                continue;
            }

            if (!excelProductNames.Add(name))
            {
                errors.Add(
                    $"Row {row}: Duplicate product name '{name}' in Excel file.");
                continue;
            }

            var product = new Product
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(description)
                    ? null
                    : description,
                Price = price,
                CategoryId = categoryId,
                IsActive = true
            };

            products.Add(product);

            var inventory = new Inventory
            {
                Product = product,
                Quantity = initialQuantity,
                ReorderLevel = reorderLevel
            };

            inventories.Add(inventory);
        }

        if (errors.Count > 0)
        {
            result.FailedRows = errors.Count;
            result.Errors = errors;

            return result;
        }

        result.ImportedRows = products.Count;

        if (products.Count == 0)
        {
            return result;
        }

        var executionStrategy = _context.Database
            .CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                await _productRepository.AddRangeAsync(products);
                await _inventoryRepository.AddRangeAsync(inventories);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        _cache.Remove("products_all");

        return result;
    }
    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Products");

        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Description";
        worksheet.Cell(1, 3).Value = "Price";
        worksheet.Cell(1, 4).Value = "CategoryId";
        worksheet.Cell(1, 5).Value = "InitialQuantity";
        worksheet.Cell(1, 6).Value = "ReorderLevel";

        worksheet.Row(1).Style.Font.Bold = true;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

}