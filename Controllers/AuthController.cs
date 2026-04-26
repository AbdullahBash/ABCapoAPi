using ABCapoAPi.Data;
using ABCapoAPi.DTOs;
using ABCapoAPi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace ABCapoAPi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(AppDbContext context, ILogger<AuthenticationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        _logger.LogInformation("Register Attempt for: " + dto.Email);

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already registered." });

        var isFirstUser = !(await _context.Users.AnyAsync());

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),

            PhoneNumber = dto.PhoneNumber,
            CreatedAt = DateTime.Now,
            IsAdmin = isFirstUser,

            // --- حفظ العنوان إذا تم إرساله ---
            Address = dto.Address,
            City = dto.City,
            CountryID = dto.CountryId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Registration successful", Id = user.Id, Name = user.Name, IsAdmin = user.IsAdmin });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        _logger.LogInformation("--- Login Attempt ---");
        _logger.LogInformation($"Identifier: {dto.Email}");

        try
        {
            // --- التعديل 1: البحث مع تجاهل حالة الأحرف (كبيرة/صغيرة) ---
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == dto.Email.ToLower() ||
                    u.Name.ToLower() == dto.Email.ToLower()
                );

            if (user == null)
            {
                _logger.LogWarning("User Not Found");
                // --- التعديل 2: إرجاع خطأ بصيغة JSON للواجهة ---
                return Unauthorized(new { message = "Invalid credentials. User not found." });
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid Password");
                // --- التعديل 2: إرجاع خطأ بصيغة JSON للواجهة ---
                return Unauthorized(new { message = "Invalid credentials. Wrong password." });
            }

            var token = TokenService.CreateToken(user.Id.ToString(), user.Email, user.IsAdmin);

            _logger.LogInformation("Login Successful");

            return Ok(new
            {
                Message = "Login successful",
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                IsAdmin = user.IsAdmin
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login Exception");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // --- دالة التحقق من المستخدم الحالي ---
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        _logger.LogInformation("--- /me Called ---");

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        _logger.LogInformation($"User Email from Token: {userEmail}");

        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("User Email is NULL! Token is likely invalid.");
            return Unauthorized(new { message = "Invalid token." });
        }

        // --- إضافة Include لجلب بيانات الدولة ---
        var user = await _context.Users
            .Include(u => u.Country)
            .FirstOrDefaultAsync(u => u.Email == userEmail);

        if (user == null)
            return NotFound(new { message = "User not found." });

        // --- إضافة الحقول الجديدة للرد ---
        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.Name,
            isAdmin = user.IsAdmin,
            address = user.Address,
            city = user.City,
            countryID = user.CountryID,
            phoneNumber = user.PhoneNumber,
            countryName = user.Country?.Name
        });
    }

    // ==========================================
    // --- دوال تسجيل الدخول الاجتماعي ---
    // ==========================================

    [HttpGet("signin-google")]
    public IActionResult SignInGoogle()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/authentication/callback"
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("signin-facebook")]
    public IActionResult SignInFacebook()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/authentication/callback"
        };

        return Challenge(properties, FacebookDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        // قراءة بيانات المصادقة من الكوكيز
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal == null)
        {
            return BadRequest(new { message = "Error logging in with external provider" });
        }

        // استخراج الايميل والاسم
        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { message = "Email claim not found." });
        }

        // البحث في قاعدة البيانات
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        // إذا لم يكن موجوداً، أنشئ حساباً جديداً
        if (user == null)
        {
            user = new User
            {
                Name = name ?? "Social User",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                CreatedAt = DateTime.Now,
                IsAdmin = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // إنشاء Token للتطبيق
        var token = TokenService.CreateToken(user.Id.ToString(), user.Email, user.IsAdmin);

        // توجيه المستخدم للواجهة الأمامية (Next.js) مع الـ Token
        var frontendUrl = "http://localhost:3000";
        return Redirect($"{frontendUrl}/login?token={token}&name={Uri.EscapeDataString(user.Name)}");
    }
}