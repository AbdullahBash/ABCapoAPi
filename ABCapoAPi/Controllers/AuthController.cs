using ABCapoAPi.Data;
using ABCapoAPi.DTOs;
using ABCapoAPi.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCapoAPi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(AppDbContext _context) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already registered.");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Registration successful", user.Id, user.Name });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var token = TokenService.CreateToken(user.Id.ToString(), user.Email);

        return Ok(new
        {
            Message = "Login successful",
            Token = token,
            UserId = user.Id,
            Name = user.Name
        });
    }
}
