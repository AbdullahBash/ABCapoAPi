using ABCapoAPi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCapoAPi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OptionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public OptionsController(AppDbContext context)
    {
        _context = context;
    }

    // 1. جلب جميع الألوان
    [HttpGet("colors")]
    public async Task<IActionResult> GetColors()
    {
        var colors = await _context.Colors.ToListAsync();
        return Ok(colors);
    }

    // 2. جلب جميع المقاسات
    [HttpGet("sizes")]
    public async Task<IActionResult> GetSizes()
    {
        var sizes = await _context.Sizes.ToListAsync();
        return Ok(sizes);
    }

    // 3. جلب جميع النسخ
    [HttpGet("copies")]
    public async Task<IActionResult> GetCopies()
    {
        var copies = await _context.Copies.ToListAsync();
        return Ok(copies);
    }
}