using PersonalFinanceTracker.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// JWT secret key'i uygulama ayarlarından alır.
var jwtKey = builder.Configuration["Jwt:Key"];

// JWT Bearer authentication ayarlarını uygulamaya ekler.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Local geliştirme aşamasında issuer kontrolü kapalı.
            ValidateIssuer = false,

            // Local geliştirme aşamasında audience kontrolü kapalı.
            ValidateAudience = false,

            // Token'ın süresinin dolup dolmadığını kontrol eder.
            ValidateLifetime = true,

            // Token'ın doğru secret key ile imzalanıp imzalanmadığını kontrol eder.
            ValidateIssuerSigningKey = true,

            // JWT token'larının doğrulanmasında kullanılacak secret key.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)
            )
        };
    });

// Yetkilendirme sistemini uygulamaya ekler.
builder.Services.AddAuthorization();

var app = builder.Build();

// Gelen isteklerde kullanıcının kimliğini JWT üzerinden doğrular.
app.UseAuthentication();

// Doğrulanan kullanıcının endpoint'e erişim yetkisini kontrol eder.
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Expense endpoint'lerini uygulamaya ekler.
app.MapExpenseEndpoints();

// Authentication işlemlerine ait endpoint'leri uygulamaya ekler.
app.MapAuthEndpoints();

// Future payment işlemlerine ait endpoint'leri uygulamaya kaydeder.
app.MapFuturePaymentEndpoints();

// Report işlemlerine ait endpoint'leri uygulamaya kaydeder.
app.MapReportEndpoints();

app.Run();