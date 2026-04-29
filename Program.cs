using System;
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

// === 1. ≈⁄œ«œ DbContext (√Ê·ÊÌ… Railway À„ Localhost) ===
// 1. ‰ Õﬁﬁ √Ê·« „‰ „ €Ì— Railway
var connectionString = builder.Configuration["DATABASE_URL"];

// 2. ≈–« ·„ ‰Ãœ° ‰»ÕÀ ›Ì „·› appsettings «·„Õ·Ì
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ”·”·… « ’«· »ﬁ«⁄œ… «·»Ì«‰« .");
}

// 3.  ”ÃÌ· «·‹ DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

// === ≈⁄œ«œ CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

// === 3. Œœ„«  «· ÿ»Ìﬁ ===
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

// === 4. ≈⁄œ«œ Middleware ===
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// === 5. „‰ÿﬁ »œ¡ «· ‘€Ì· (≈’·«Õ ‰Â«∆Ì ··≈‰‘«¡ Ê«·« ’«·) ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // --- «·„—Õ·… «·√Ê·Ï: „Õ«Ê·… «·« ’«· »«·Œ«œ„ (Retry Logic) ---
    int maxRetries = 5;
    int delayMs = 5000;

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation($"„Õ«Ê·… «·« ’«· »Œ«œ„ ﬁ«⁄œ… «·»Ì«‰« ... ({i + 1}/{maxRetries})");
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation(" „ «·« ’«· »Œ«œ„ ﬁ«⁄œ… «·»Ì«‰«  »‰Ã«Õ!");
                break; // ‰ÃÕ «·« ’«·° ‰Œ—Ã „‰ «·Õ·ﬁ…
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"›‘· «·« ’«· »«·„Õ«Ê·… {i + 1}: {ex.Message}");
        }

        if (i < maxRetries - 1)
        {
            logger.LogInformation("ﬁ«⁄œ… «·»Ì«‰«  €Ì— Ã«Â“… »⁄œ. «·«‰ Ÿ«— 5 ÀÊ«‰Ú...");
            await Task.Delay(delayMs);
        }
        else
        {
            // ≈–« ›‘· «·« ’«·  „«„« »⁄œ «·„Õ«Ê·« ° ‰ Êﬁ› Â‰«
            logger.LogError("·„ ‰ „ﬂ‰ „‰ «·« ’«· »ﬁ«⁄œ… «·»Ì«‰« .");
            throw new Exception("›‘· «·« ’«· »ﬁ«⁄œ… «·»Ì«‰« .");
        }
    }

    // --- «·„—Õ·… «·À«‰Ì…: ≈‰‘«¡ «·Ãœ«Ê· (Ì „ „—… Ê«Õœ… ›ﬁÿ »⁄œ ‰Ã«Õ «·« ’«·) ---
    try
    {
        logger.LogInformation("Ã«—Ì «· Õﬁﬁ „‰ «·Ãœ«Ê· Ê≈‰‘«∆Â« ≈–« ·“„ «·√„—...");
        db.Database.EnsureCreated();
        logger.LogInformation(" „ «· √ﬂœ „‰ ﬁ«⁄œ… «·»Ì«‰«  Ê«·Ãœ«Ê· »‰Ã«Õ.");
    }
    catch (Exception ex)
    {
        // ›Ì Õ«· ÊÃÊœ Œÿ√ ›Ì »‰Ì… ﬁ«⁄œ… «·»Ì«‰« ° ‰ÿ»⁄ «·Œÿ√ ·„⁄—›… «·”»»
        logger.LogError($"ÕœÀ Œÿ√ √À‰«¡ ≈‰‘«¡ «·Ãœ«Ê·: {ex.Message}");
        // Ì„ﬂ‰ﬂ ≈“«·… «·”ÿ— «· «·Ì ≈–« ﬂ‰   —Ìœ  Ã«Â· √Œÿ«¡ «·„Œÿÿ Ê«·«” „—«—
        throw;
    }
}

app.Run();