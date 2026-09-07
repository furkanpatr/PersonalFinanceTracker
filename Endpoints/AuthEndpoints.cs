using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using PersonalFinanceTracker.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PersonalFinanceTracker.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (
            RegisterRequest request,
            IConfiguration configuration) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Surname) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new
                {
                    message = "Ad, soyad, email ve şifre alanları boş olamaz"
                });
            }

            if (request.Password.Length < 6)
            {
                return Results.BadRequest(new
                {
                    message = "Şifre en az 6 karakter olmalıdır"
                });
            }

            if (!request.Email.Contains("@"))
            {
                return Results.BadRequest(new
                {
                    message = "Geçerli bir email adresi giriniz"
                });
            }

            try
            {
                var connectionString =
                    configuration.GetConnectionString("DefaultConnection");

                await using var connection =
                    new NpgsqlConnection(connectionString);

                await connection.OpenAsync();

                var checkEmailSql = """
                    SELECT COUNT(*)
                    FROM users
                    WHERE email = @email;
                    """;

                await using var checkEmailCommand =
                    new NpgsqlCommand(checkEmailSql, connection);

                checkEmailCommand.Parameters.AddWithValue(
                    "email",
                    request.Email
                );

                var emailCount =
                    Convert.ToInt32(
                        await checkEmailCommand.ExecuteScalarAsync()
                    );

                if (emailCount > 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "Bu email adresi zaten kayıtlı"
                    });
                }

                var passwordHasher =
                    new PasswordHasher<object>();

                var passwordHash =
                    passwordHasher.HashPassword(
                        null!,
                        request.Password
                    );

                var insertUserSql = """
                    INSERT INTO users
                        (name, surname, phone, email, password_hash)
                    VALUES
                        (@name, @surname, @phone, @email, @passwordHash)
                    RETURNING id;
                    """;

                await using var insertUserCommand =
                    new NpgsqlCommand(insertUserSql, connection);

                insertUserCommand.Parameters.AddWithValue(
                    "name",
                    request.Name
                );

                insertUserCommand.Parameters.AddWithValue(
                    "surname",
                    request.Surname
                );

                insertUserCommand.Parameters.AddWithValue(
                    "phone",
                    (object?)request.Phone ?? DBNull.Value
                );

                insertUserCommand.Parameters.AddWithValue(
                    "email",
                    request.Email
                );

                insertUserCommand.Parameters.AddWithValue(
                    "passwordHash",
                    passwordHash
                );

                var newUserId =
                    Convert.ToInt32(
                        await insertUserCommand.ExecuteScalarAsync()
                    );

                return Results.Json(
                    new
                    {
                        id = newUserId,
                        message = "Kullanıcı başarıyla oluşturuldu"
                    },
                    statusCode: StatusCodes.Status201Created
                );
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Results.BadRequest(new
                {
                    message = "Bu email adresi zaten kayıtlı"
                });
            }
            catch
            {
                return Results.Problem(
                    "Kullanıcı oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        });

        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            IConfiguration configuration) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new
                {
                    message = "Email ve şifre alanları boş olamaz"
                });
            }

            try
            {
                var connectionString =
                    configuration.GetConnectionString("DefaultConnection");

                await using var connection =
                    new NpgsqlConnection(connectionString);

                await connection.OpenAsync();

                var sql = """
                    SELECT
                        id,
                        email,
                        password_hash
                    FROM users
                    WHERE email = @email;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "email",
                    request.Email
                );

                await using var reader =
                    await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Results.Unauthorized();
                }

                var userId =
                    reader.GetInt32(0);

                var email =
                    reader.GetString(1);

                var passwordHash =
                    reader.GetString(2);

                var passwordHasher =
                    new PasswordHasher<object>();

                var passwordResult =
                    passwordHasher.VerifyHashedPassword(
                        null!,
                        passwordHash,
                        request.Password
                    );

                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    return Results.Unauthorized();
                }

                var jwtKey =
                    configuration["Jwt:Key"];

                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userId.ToString()
                    ),
                    new Claim(
                        ClaimTypes.Email,
                        email
                    )
                };

                var securityKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey!)
                    );

                var credentials =
                    new SigningCredentials(
                        securityKey,
                        SecurityAlgorithms.HmacSha256
                    );

                var token =
                    new JwtSecurityToken(
                        claims: claims,
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: credentials
                    );

                var tokenString =
                    new JwtSecurityTokenHandler()
                        .WriteToken(token);

                return Results.Ok(new
                {
                    id = userId,
                    email = email,
                    token = tokenString,
                    message = "Giriş başarılı"
                });
            }
            catch
            {
                return Results.Problem(
                    "Giriş işlemi sırasında beklenmeyen bir hata oluştu."
                );
            }
        });
    }
}