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

// 1. ≈⁄œ«œ DbContext (≈Ã»«—Ì ·‹ Postgres - Õ· ‰Â«∆Ì ·„‘ﬂ·… Railway)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RAILWAY_DATABASE_URL") ?? builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. ≈⁄œ«œ «·„’«œﬁ… (JWT + Social Login)
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

// ≈⁄œ«œ CORS („› ÊÕ ··Ã„Ì⁄ ·Õ· „‘ﬂ·… Railway Ê«·Ê«ÃÂ…)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()       // Ì”„Õ »√Ì „Êﬁ⁄ (Railway, Localhost, Vercel)
            .AllowAnyMethod()       // Ì”„Õ »‹ GET, POST, DELETE...
            .AllowAnyHeader()       // Ì”„Õ »√Ì Headers
    );
});

// 3. Œœ„«  «· ÿ»Ìﬁ
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 4. ≈⁄œ«œ «· ŒÊÌ·
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. ≈⁄œ«œ Middleware
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

// --- ÿ»«⁄… —«»ÿ «·« ’«· ·· Õﬁﬁ „‰ «·„‘ﬂ·… ---
var connString = builder.Configuration.GetConnectionString("RAILWAY_DATABASE_URL") ?? builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"[DEBUG] Connection String is: {connString ?? "NULL"}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //  „ ≈Ìﬁ«› EnsureCreated „ƒﬁ « · Ã‰» «·«‰ÂÌ«—
    // db.Database.EnsureCreated(); 
}

app.Run();