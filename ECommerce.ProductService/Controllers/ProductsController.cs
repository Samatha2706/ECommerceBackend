using ECommerce.ProductService.DTOs.Products;
using ECommerce.ProductService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        string? sortOrder = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await _productService.GetAllAsync(
            search,
            categoryId,
            minPrice,
            maxPrice,
            sortBy,
            sortOrder,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    [HttpGet("bulk-upload/template")]
    public IActionResult DownloadBulkTemplate()
    {
        var fileBytes = _productService.GenerateBulkTemplate();

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "BulkProductsTemplate.xlsx");
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductDto dto)
    {
        try
        {
            var product = await _productService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateProductDto dto)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, dto);

            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBulk(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please upload a valid Excel file."
            });
        }

        try
        {
            using var stream = file.OpenReadStream();

            var result = await _productService.UploadBulkAsync(stream);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _productService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}