using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. JWT Doğrulama Servisi
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PersonalVault",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PersonalVaultUsers",
        // Bunu bul ve parantez içini şöyle değiştir:
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Bu_Cok_Gizli_Ve_En_Az_32_Karakter_123!")),
        ClockSkew = TimeSpan.Zero 
    };
});

builder.Services.AddControllers();
builder.Services.AddCors();

var app = builder.Build();

// 2. Güvenlik ve Middleware Sıralaması
app.UseCors(opt => opt.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthentication(); 
app.UseAuthorization();  

app.MapControllers();
app.Run();