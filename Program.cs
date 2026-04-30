using System;
using System.Linq; // ÷—Ê—Ì ·⁄„·Ì…  Õ·Ì· «·—«»ÿ
using ABCapoAPi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// ===  ÕœÌœ „Ã·œ wwwroot ===
builder.WebHost.UseWebRoot("wwwroot");

// === 1. ≈⁄œ«œ DbContext ( ÕÊÌ· —«»ÿ Railway Ê÷»ÿ «·« ’«·) ===
var connectionString = builder.Configuration["DATABASE_URL"];

// ≈–« ·„ ÌÊÃœ „ €Ì— «·»Ì∆… (Ì⁄„· „Õ·Ì«)° ‰√Œ– „‰ appsettings
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ”·”·… « ’«· ﬁ«⁄œ… «·»Ì«‰« .");
}

// === Õ· „‘ﬂ·… Format of initialization string ===
// ≈–« ﬂ«‰ «·—«»ÿ »’Ì€… Railway (postgres://...) ‰ﬁÊ„ » ÕÊÌ·Â ≈·Ï ’Ì€… ﬁÌ«”Ì… (Host=...)
if (connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');

        var username = userInfo[0];
        // ‰Ã„⁄ »«ﬁÌ «·√Ã“«¡ ≈–« ﬂ«‰ «·»«”Ê—œ ÌÕ ÊÌ ⁄·Ï ‰ﬁÿ Ì‰ (:)
        var password = userInfo.Length > 1 ? string.Join(":", userInfo.Skip(1)) : "";
        var database = uri.AbsolutePath.TrimStart('/');

        // ≈–« ·„ Ì–ﬂ— «”„ ﬁ«⁄œ… «·»Ì«‰«  ›Ì «·—«»ÿ° ‰÷⁄ «”„ «› —«÷Ì (railway)
        if (string.IsNullOrEmpty(database)) database = "railway";

        // »‰«¡ «·—«»ÿ «·ÃœÌœ «·„ Ê«›ﬁ „⁄ Npgsql
        connectionString = $"Host={uri.Host};Port={uri.Port};Username={username};Password={password};Database={database};SSL Mode=Require;TrustServerCertificate=True";
    }
    catch
    {
        // ≈–« ›‘· «· ÕÊÌ·° ‰” Œœ„ «·—«»ÿ «·√’·Ì
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
        npgsqlOptions.CommandTimeout(30);
    });
});

// === 2. ≈⁄œ«œ «·„’«œﬁ… (JWT + Social Login) ===
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("abbaFireEnd@2025SuperSecretKeyForJWTGeneration!"))
    };
})
.AddCookie(options =>
{
    options.LoginPath = "/api/Authentication/login";
    options.LogoutPath = "/api/Authentication/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
    options.CallbackPath = "/signin-google";
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "YOUR_FACEBOOK_APP_ID";
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "YOUR_FACEBOOK_APP_SECRET";
    options.CallbackPath = "/signin-facebook";
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// === 3. ≈⁄œ«œ Middleware ===
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// === 4. „‰ÿﬁ »œ¡ «· ‘€Ì· (Õ·ﬁ… ·«‰Â«∆Ì… Õ Ï ‰Ã«Õ «·« ’«·) ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    bool dbReady = false;
    int retryCount = 0;

    while (!dbReady)
    {
        try
        {
            retryCount++;
            logger.LogInformation($"„Õ«Ê·…  ÂÌ∆… ﬁ«⁄œ… «·»Ì«‰« ... („Õ«Ê·… —ﬁ„ {retryCount})");

            // „Õ«Ê·… ≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰«  Ê«·Ãœ«Ê·
            db.Database.EnsureCreated();

            logger.LogInformation(" „ «·« ’«· »ﬁ«⁄œ… «·»Ì«‰«  Ê≈‰‘«¡ «·Ãœ«Ê· »‰Ã«Õ!");
            dbReady = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning($"›‘· «·« ’«· √Ê «·≈‰‘«¡: {ex.Message}");
            logger.LogInformation("«‰ Ÿ«— 5 ÀÊ«‰Ú ··„Õ«Ê·… „—… √Œ—Ï...");

            // «‰ Ÿ«— ﬁ»· ≈⁄«œ… «·„Õ«Ê·… (·‰ Ì‰Â«— «· ÿ»Ìﬁ)
            await Task.Delay(5000);
        }
    }
}

app.Run();