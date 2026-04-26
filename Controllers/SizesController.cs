using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCapoAPi.Data;

namespace ABCapoAPi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SizesController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Size>>> GetSizes()
    {
        return await _context.Sizes.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Size>> CreateSize(Size size)
    {
        _context.Sizes.Add(size);
        await _context.SaveChangesAsync();
        // تم التعديل: استخدام SizeID بدلاً من sizeID
        return CreatedAtAction(nameof(GetSizes), new { id = size.SizeID }, size);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSize(int id, Size size)
    {
        // تم التعديل: استخدام SizeID بدلاً من sizeID
        if (id != size.SizeID)
        {
            return BadRequest();
        }

        _context.Entry(size).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SizeExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSize(int id)
    {
        var size = await _context.Sizes.FindAsync(id);
        if (size == null)
        {
            return NotFound();
        }

        _context.Sizes.Remove(size);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SizeExists(int id)
    {
        // تم التعديل: استخدام SizeID بدلاً من sizeID
        return _context.Sizes.Any(e => e.SizeID == id);
    }
}