using ECommerce.ProductService.Data;
using ECommerce.ProductService.DTOs.Categories;
using ECommerce.ProductService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ProductDbContext _context;

    public CategoriesController(ProductDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .FirstOrDefaultAsync();

        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        var name = dto.Name.Trim();

        if (await _context.Categories.AnyAsync(c => c.Name == name))
            return Conflict(new { message = "Category already exists." });

        var category = new Category
        {
            Name = name,
            Description = dto.Description?.Trim()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category is null)
            return NotFound();

        var name = dto.Name.Trim();

        if (await _context.Categories.AnyAsync(
            c => c.Id != id && c.Name == name))
            return Conflict(new { message = "Category already exists." });

        category.Name = name;
        category.Description = dto.Description?.Trim();

        await _context.SaveChangesAsync();

        return Ok(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category is null)
            return NotFound();

        if (await _context.Products.AnyAsync(p => p.CategoryId == id))
            return BadRequest(new
            {
                message = "Cannot delete a category that has products."
            });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
