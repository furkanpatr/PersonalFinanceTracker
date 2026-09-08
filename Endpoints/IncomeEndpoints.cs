using Npgsql;
using PersonalFinanceTracker.Api.Models;
using System.Security.Claims;

namespace PersonalFinanceTracker.Api.Endpoints;

public static class IncomeEndpoints
{
    public static void MapIncomeEndpoints(this WebApplication app)
    {
        // Veritabanındaki kullanıcının tüm gelirlerini getiren endpoint
        app.MapGet("/api/incomes", async (
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
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
                        user_id,
                        income_category_id,
                        amount,
                        date,
                        description
                    FROM incomes
                    WHERE user_id = @userId
                    ORDER BY id;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                var incomes = new List<IncomeResponse>();

                while (await reader.ReadAsync())
                {
                    incomes.Add(new IncomeResponse(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetInt32(2),
                        reader.GetDecimal(3),
                        DateOnly.FromDateTime(reader.GetDateTime(4)),
                        reader.IsDBNull(5) ? null : reader.GetString(5)
                    ));
                }

                return Results.Ok(incomes);
            }
            catch
            {
                return Results.Problem(
                    "Gelirler getirilirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        // Yeni geliri veritabanına ekleyen endpoint
        app.MapPost("/api/incomes", async (
            IncomeRequest income,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (income.Amount <= 0 ||
                income.IncomeCategoryId <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Tutar ve kategori ID değeri 0'dan büyük olmalıdır"
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
                    INSERT INTO incomes
                        (user_id, income_category_id, amount, date, description)
                    VALUES
                        (@userId, @incomeCategoryId, @amount, @date, @description)
                    RETURNING id;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue(
                    "incomeCategoryId",
                    income.IncomeCategoryId);
                command.Parameters.AddWithValue("amount", income.Amount);
                command.Parameters.AddWithValue("date", income.Date);
                command.Parameters.AddWithValue(
                    "description",
                    (object?)income.Description ?? DBNull.Value);

                var newId =
                    (int)(await command.ExecuteScalarAsync())!;

                return Results.Created(
                    $"/api/incomes/{newId}",
                    new
                    {
                        id = newId,
                        message = "Gelir başarıyla eklendi"
                    }
                );
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen IncomeCategoryId geçerli değil"
                });
            }
            catch
            {
                return Results.Problem(
                    "Gelir eklenirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        // Geliri güncelleyen endpoint
        app.MapPut("/api/incomes/{id}", async (
            int id,
            IncomeRequest income,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Geçerli bir gelir ID değeri gönderilmelidir"
                });
            }

            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (income.Amount <= 0 ||
                income.IncomeCategoryId <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Tutar ve kategori ID değeri 0'dan büyük olmalıdır"
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
                    UPDATE incomes
                    SET
                        income_category_id = @incomeCategoryId,
                        amount = @amount,
                        date = @date,
                        description = @description
                    WHERE id = @id
                    AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue(
                    "incomeCategoryId",
                    income.IncomeCategoryId);
                command.Parameters.AddWithValue("amount", income.Amount);
                command.Parameters.AddWithValue("date", income.Date);
                command.Parameters.AddWithValue(
                    "description",
                    (object?)income.Description ?? DBNull.Value);

                var affectedRows =
                    await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Gelir bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Gelir başarıyla güncellendi"
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen IncomeCategoryId geçerli değil"
                });
            }
            catch
            {
                return Results.Problem(
                    "Gelir güncellenirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapDelete("/api/incomes/{id}", async (
            int id,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Geçerli bir gelir ID değeri gönderilmelidir"
                });
            }

            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var connectionString =
                    configuration.GetConnectionString("DefaultConnection");

                await using var connection =
                    new NpgsqlConnection(connectionString);

                await connection.OpenAsync();

                var sql = """
                    DELETE FROM incomes
                    WHERE id = @id
                    AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);

                var affectedRows =
                    await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Gelir bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Gelir başarıyla silindi"
                });
            }
            catch
            {
                return Results.Problem(
                    "Gelir silinirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

    }
}