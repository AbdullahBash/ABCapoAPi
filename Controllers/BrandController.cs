using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCapoAPi.Data;

namespace ABCapoAPi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandController : ControllerBase
{
    private readonly AppDbContext _context;

    public BrandController(AppDbContext context)
    {
        _context = context;
    }

    // DTO
    public class BrandDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BrandDto>>> GetBrands()
    {
        var brands = await _context.Brands.ToListAsync();

        return Ok(brands.Select(b => new BrandDto
        {
            Id = b.Id,
            Name = b.Name
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BrandDto>> GetBrand(int id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand is null)
            return NotFound();

        return new BrandDto { Id = brand.Id, Name = brand.Name };
    }

    [HttpPost]
    public async Task<ActionResult<BrandDto>> CreateBrand(Brand brand)
    {
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        var dto = new BrandDto { Id = brand.Id, Name = brand.Name };

        return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBrand(int id, Brand brand)
    {
        if (id != brand.Id)
            return BadRequest();

        _context.Entry(brand).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException) when (!_context.Brands.Any(b => b.Id == id))
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand is null)
            return NotFound();

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
