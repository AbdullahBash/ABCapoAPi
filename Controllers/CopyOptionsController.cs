using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCapoAPi.Data;

namespace ABCapoAPi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CopyOptionsController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CopyOption>>> GetCopyOptions()
    {
        // تأكد أن اسم الـ DbSet في الـ Context هو Copies
        return await _context.Copies.ToListAsync();
    }

    // إضافة: دالة إنشاء نسخة جديدة
    [HttpPost]
    public async Task<ActionResult<CopyOption>> CreateCopy(CopyOption copy)
    {
        _context.Copies.Add(copy);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCopyOptions), new { id = copy.CopyID }, copy);
    }

    // إضافة: دالة تعديل نسخة
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCopy(int id, CopyOption copy)
    {
        if (id != copy.CopyID)
        {
            return BadRequest();
        }

        _context.Entry(copy).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CopyExists(id))
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

    // إضافة: دالة حذف نسخة (اختياري)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCopy(int id)
    {
        var copy = await _context.Copies.FindAsync(id);
        if (copy == null)
        {
            return NotFound();
        }

        _context.Copies.Remove(copy);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CopyExists(int id)
    {
        return _context.Copies.Any(e => e.CopyID == id);
    }
}