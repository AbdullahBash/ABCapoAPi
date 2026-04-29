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

// === 1. ≈⁄œ«œ DbContext («·Õ· «·‰Â«∆Ì ·„‘«ﬂ· Railway SSL) ===
var connectionString = builder.Configuration["DATABASE_URL"];

if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ”·”·… « ’«· ﬁ«⁄œ… «·»Ì«‰« .");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Â–« «·”ÿ— ÷—Ê—Ì Ãœ« ·Õ· „‘ﬂ·… (EndOfStreamException) ›Ì Railway
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

// === 4. „‰ÿﬁ »œ¡ «· ‘€Ì· (Retry Logic + EnsureCreated) ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int maxRetries = 5;
    int delayMs = 5000;

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation($"„Õ«Ê·… «·« ’«· »ﬁ«⁄œ… «·»Ì«‰« ... ({i + 1}/{maxRetries})");
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation(" „ «·« ’«· »ﬁ«⁄œ… «·»Ì«‰«  »‰Ã«Õ!");
                break;
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
            logger.LogError("›‘· «·« ’«· »ﬁ«⁄œ… «·»Ì«‰« .");
            throw new Exception("›‘· «·« ’«·.");
        }
    }

    try
    {
        logger.LogInformation("Ã«—Ì «· Õﬁﬁ „‰ «·Ãœ«Ê· Ê≈‰‘«∆Â«...");
        db.Database.EnsureCreated();
        logger.LogInformation(" „ «· √ﬂœ „‰ ﬁ«⁄œ… «·»Ì«‰«  Ê«·Ãœ«Ê· »‰Ã«Õ.");
    }
    catch (Exception ex)
    {
        logger.LogError($"Œÿ√ √À‰«¡ ≈‰‘«¡ «·Ãœ«Ê·: {ex.Message}");
        throw;
    }
}

app.Run();