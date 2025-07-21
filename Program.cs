using ABCapoAPi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. ≈⁄œ«œ DbContext „⁄ ﬁ—«¡… «·« ’«· „‰ 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. ﬁ—«¡… „› «Õ JWT „‰ «·≈⁄œ«œ« 
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("JWT key is missing. Please set 'Jwt:Key' in configuration.");
}

// 3. ≈⁄œ«œ «·„’«œﬁ… ⁄»— JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 4. ≈⁄œ«œ «·Œœ„«  «·√Œ—Ï
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. ≈⁄œ«œ «·»«Ì»·«Ì‰
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ··”„«Õ »«·Ê’Ê· ·‹ /images/

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
