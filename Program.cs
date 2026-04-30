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

// === 1. ≈⁄œ«œ DbContext („⁄ «· ‘ŒÌ’ Diagnostics) ===
Console.WriteLine("--- DEBUG: »œ¡ ›Õ’ «·« ’«· »ﬁ«⁄œ… «·»Ì«‰«  ---");

var connectionString = builder.Configuration["DATABASE_URL"];
Console.WriteLine($"Â·  „ «·⁄ÀÊ— ⁄·Ï DATABASE_URLø {(!string.IsNullOrEmpty(connectionString))}");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ALERT: ·„ Ì „ «·⁄ÀÊ— ⁄·Ï DATABASE_URL. ”Ì „ «” Œœ«„ DefaultConnection.");
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"ﬁÌ„… DefaultConnection (√Ê· 50 Õ—›): {connectionString?.Substring(0, Math.Min(50, connectionString.Length))}...");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ”·”·… « ’«· ﬁ«⁄œ… «·»Ì«‰« .");
}

Console.WriteLine($"”Ì „ «” Œœ«„ «·« ’«·: {connectionString?.Substring(0, Math.Min(50, connectionString.Length))}...");
Console.WriteLine("-----------------------------------------------");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5);
        npgsqlOptions.CommandTimeout(30);
    });
});

// === 2. ≈⁄œ«œ «·„’«œﬁ… ===
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// === 4. „‰ÿﬁ »œ¡ «· ‘€Ì· ===
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
            logger.LogInformation($"„Õ«Ê·… «·« ’«·... ({retryCount})");
            db.Database.EnsureCreated();
            logger.LogInformation(" „ «·« ’«· »‰Ã«Õ!");
            dbReady = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning($"›‘· «·« ’«·: {ex.Message}");
            await Task.Delay(5000);
        }
    }
}

app.Run();