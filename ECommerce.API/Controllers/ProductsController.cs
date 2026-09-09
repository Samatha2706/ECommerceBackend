using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    private readonly IBulkProductUploadService _bulkProductUploadService;

    public ProductsController(
    IProductService productService,
    IBulkProductUploadService bulkProductUploadService)
    {
        _productService = productService;
        _bulkProductUploadService = bulkProductUploadService;
    }

    // GET: api/products
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll([FromQuery] string? search = null, 
        [FromQuery] int? categoryId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy=null,
        [FromQuery] string? sortOrder=null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var products = await _productService.GetAllAsync(search, categoryId, minPrice, maxPrice,sortBy,sortOrder, pageNumber, pageSize);

        return Ok(products);
    }

    // GET: api/products/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        return Ok(product);
    }

    // POST: api/products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto createProductDto)
    {
        try
        {
            var product =
                await _productService.CreateAsync(createProductDto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                product);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // PUT: api/products/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        [FromBody] UpdateProductDto updateProductDto)
    {
        try
        {
            var product =
                await _productService.UpdateAsync(
                    id,
                    updateProductDto);

            if (product is null)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _productService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        return NoContent();
    }

    [HttpGet("bulk-upload/template")]
    [Authorize(Roles = "Admin")]
    public IActionResult DownloadBulkUploadTemplate()
    {
        var fileBytes = _bulkProductUploadService.GenerateTemplate();

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "BulkProductsTemplate.xlsx");
    }


    [HttpPost("bulk-upload")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BulkProductUploadResultDto>> BulkUpload(
    IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please upload a valid Excel file."
            });
        }

        if (!Path.GetExtension(file.FileName)
            .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Only .xlsx Excel files are supported."
            });
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var result = await _bulkProductUploadService
                .UploadAsync(stream);

            if (result.Errors.Count > 0)
            {
                return BadRequest(result);
            }

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
}