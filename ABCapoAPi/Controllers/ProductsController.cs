using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCapoAPi.Data;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ABCapoAPi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var products = await _context.Products.ToListAsync();

        foreach (var p in products)
        {
            if (!string.IsNullOrWhiteSpace(p.ImageUrl) && !p.ImageUrl.StartsWith("http"))
                p.ImageUrl = baseUrl + p.ImageUrl;
        }

        return products;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product is null ? NotFound() : product;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest();

        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException) when (!ProductExists(id))
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var imageUrl = $"/images/{fileName}";
        return Ok(new { ImageUrl = imageUrl });
    }

    private bool ProductExists(int id) =>
        _context.Products.Any(e => e.Id == id);
}
