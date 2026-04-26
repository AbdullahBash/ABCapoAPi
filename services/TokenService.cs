using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ABCapoAPi.Services;

public static class TokenService
{
    // تم تحديث الدالة لاستقبال حالة الأدمن (isAdmin)
    public static string CreateToken(string userId, string userEmail, bool isAdmin)
    {
        // نفس المفتاح السري الذي استخدمته سابقاً
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("abbaFireEnd@2025SuperSecretKeyForJWTGeneration!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, userEmail),
            // --- هنا نضيف الدور (Role) داخل التوكن ---
            // إذا كان أدمن سنضع "Admin"، وإذا كان مستخدم عادي سنضع "User"
            new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3), // تم زيادة المدة لثلاث ساعات
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}