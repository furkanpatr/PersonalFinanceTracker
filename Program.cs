using PersonalFinanceTracker.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var jwtKey = builder.Configuration["Jwt:Key"];
    builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)
            )
        };
    });

    builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Ana endpoint
app.MapGet("/", () =>
{
    return "Personal Finance Tracker API çalışıyor";
});

// API durumunu kontrol eden endpoint
app.MapGet("/api/status", () =>
{
    return "API durumu: Aktif";
});

// Uygulama hakkında temel bilgileri döndüren endpoint
app.MapGet("/api/info", () =>
{
    return new
    {
        status = "Aktif",
        uygulama = "Personal Finance Tracker",
        surum = "1.0"
    };
});

// ID ile kullanıcı bilgisi istemek için kullanılan endpoint
app.MapGet("/api/users/{id}", (int id) =>
{
    return $"İstenen kullanıcı ID: {id}";
});

// Durum ve minimum tutara göre gelecek ödemeleri filtreleyen endpoint
app.MapGet("/api/future-payments", (string status, decimal minAmount) =>
{
    return $"Durum: {status}, Minimum tutar: {minAmount}";
});


// Expense endpoint'lerini uygulamaya ekler.
app.MapExpenseEndpoints();
// Authentication işlemlerine ait endpoint'leri uygulamaya ekler.
app.MapAuthEndpoints();
app.Run();
