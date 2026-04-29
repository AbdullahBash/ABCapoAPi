using System;
using ABCapoAPi.Data; //  √ﬂœ √‰ Â–« «·«”„ Ì ÿ«»ﬁ „⁄ «”„ „”«Õ… «·«”„ «·Œ«’… »ﬂ (Namespace)
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

// === 1. ≈⁄œ«œ DbContext (≈’·«Õ ‰Â«∆Ì ·„‘ﬂ·… Railway) ===
// ‰Õ«Ê· ﬁ—«¡… «·”·”·… «·«› —«÷Ì… (··⁄„· «·„Õ·Ì)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ≈–« ·„ ‰Ãœ° ‰»ÕÀ ⁄‰ „ €Ì— «·»Ì∆… «·Œ«’ »‹ Railway „»«‘—… (DATABASE_URL)
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration["DATABASE_URL"];
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ”·”·… « ’«· »ﬁ«⁄œ… «·»Ì«‰« .  √ﬂœ „‰ „ €Ì— DATABASE_URL ›Ì Railway.");
}

//  ”ÃÌ· «·‹ DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// === 2. ≈⁄œ«œ «·„’«œﬁ… (JWT + Social Login) ===
builder.Services.AddAuthentication(options =>
{
    // ‰ﬁÊ„ »Ã⁄· JWT ÂÊ «·«› —«÷Ì ·Ê«ÃÂ… «·‹ API
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

        // --- «·„› «Õ «·„ÊÕœ („ÿ«»ﬁ ·‹ TokenService) ---
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("abbaFireEnd@2025SuperSecretKeyForJWTGeneration!"))
    };
})
// ≈÷«›… Cookie Authentication (÷—Ê—Ì · œ›ﬁ OAuth2 «·Œ«—ÃÌ)
.AddCookie(options =>
{
    options.LoginPath = "/api/Authentication/login"; // „”«— ≈⁄«œ… «· ÊÃÌÂ ≈–« ·„ Ìﬂ‰ „”Ã· œŒÊ·
    options.LogoutPath = "/api/Authentication/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
})
// ≈⁄œ«œ Google
.AddGoogle(options =>
{
    // Ì „ ﬁ—«¡… Â–Â «·ﬁÌ„ „‰ appsettings.Development.json
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
    options.CallbackPath = "/signin-google";
})
// ≈⁄œ«œ Facebook
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "YOUR_FACEBOOK_APP_ID";
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "YOUR_FACEBOOK_APP_SECRET";
    options.CallbackPath = "/signin-facebook";
});

// === ≈⁄œ«œ CORS („› ÊÕ ··Ã„Ì⁄ ·Õ· „‘ﬂ·… Railway Ê«·Ê«ÃÂ…) ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()       // Ì”„Õ »√Ì „Êﬁ⁄ (Railway, Localhost, Vercel)
            .AllowAnyMethod()       // Ì”„Õ »‹ GET, POST, DELETE...
            .AllowAnyHeader()       // Ì”„Õ »√Ì Headers
    );
});

// === 3. Œœ„«  «· ÿ»Ìﬁ ===
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// === 4. ≈⁄œ«œ «· ŒÊÌ· ===
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// === 5. ≈⁄œ«œ Middleware ===
app.UseSwagger();
app.UseSwaggerUI();

//  ›⁄Ì· HTTPS Redirect ··”Ì—›—«  «·ÕﬁÌﬁÌ… („À· Railway)
app.UseHttpsRedirection();

app.UseCors("AllowAll");

// ≈⁄œ«œ «·’Ê—
app.UseStaticFiles();

// «· — Ì» „Â„: Authentication ﬁ»· Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// === 6. „‰ÿﬁ »œ¡ «· ‘€Ì· „⁄ ≈⁄«œ… «·„Õ«Ê·… (Õ· „‘ﬂ·… «·«‰ÂÌ«— EndOfStreamException) ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int maxRetries = 5;       // „Õ«Ê·… 5 „—« 
    int delayMs = 5000;      // «‰ Ÿ«— 5 ÀÊ«‰Ì »Ì‰ «·„Õ«Ê·« 

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation($"„Õ«Ê·… —ﬁ„ {i + 1}: Ã«—Ì «·« ’«· »ﬁ«⁄œ… «·»Ì«‰« ...");

            // ‰” Œœ„ CanConnectAsync ·· Õﬁﬁ »‘ﬂ· €Ì— „ “«„‰
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation(" „ «·« ’«· »‰Ã«Õ! Ã«—Ì ≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰«  ≈–« ·“„ «·√„—...");
                db.Database.EnsureCreated();
                break; // ‰ÃÕ «·« ’«·° ‰Œ—Ã „‰ «·Õ·ﬁ…
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"›‘·  «·„Õ«Ê·… —ﬁ„ {i + 1}: {ex.Message}");
        }

        // ≈–« ·„  ﬂ‰ «·„Õ«Ê·… «·√ŒÌ—…° ‰‰ Ÿ— Ê‰Õ«Ê· „—… √Œ—Ï
        if (i < maxRetries - 1)
        {
            logger.LogInformation($"ﬁ«⁄œ… «·»Ì«‰«  €Ì— Ã«Â“… »⁄œ. «·«‰ Ÿ«— ·„œ… {delayMs / 1000} ÀÊ«‰Ú...");
            await Task.Delay(delayMs);
        }
        else
        {
            logger.LogError("Œÿ√ ›«œÕ: ›‘· «·« ’«· »ﬁ«⁄œ… «·»Ì«‰«  »⁄œ ⁄œ… „Õ«Ê·« .");
            // Ì„ﬂ‰ﬂ —„Ì «” À‰«¡ ·≈Ìﬁ«› «· ÿ»Ìﬁ »«·ﬂ«„· √Ê  —ﬂÂ ÌÕ«Ê·
            throw new Exception(" ⁄–— «·« ’«· »ﬁ«⁄œ… «·»Ì«‰«  ›Ì Railway.");
        }
    }
}

app.Run();