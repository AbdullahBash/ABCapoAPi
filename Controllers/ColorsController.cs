using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCapoAPi.Data;

namespace ABCapoAPi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ColorsController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Color>>> GetColors()
    {
        return await _context.Colors.ToListAsync();
    }

    // إضافة: دالة إنشاء لون جديد
    [HttpPost]
    public async Task<ActionResult<Color>> CreateColor(Color color)
    {
        _context.Colors.Add(color);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetColors), new { id = color.ColorID }, color);
    }

    // إضافة: دالة تعديل لون
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateColor(int id, Color color)
    {
        if (id != color.ColorID)
        {
            return BadRequest();
        }

        _context.Entry(color).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ColorExists(id))
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

    // إضافة: دالة حذف لون (اختياري)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteColor(int id)
    {
        var color = await _context.Colors.FindAsync(id);
        if (color == null)
        {
            return NotFound();
        }

        _context.Colors.Remove(color);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ColorExists(int id)
    {
        return _context.Colors.Any(e => e.ColorID == id);
    }
}