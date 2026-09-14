using Npgsql;
using PersonalFinanceTracker.Api.Models;
using System.Security.Claims;

namespace PersonalFinanceTracker.Api.Endpoints;

public static class BudgetLimitEndpoints
{
    public static void MapBudgetLimitEndpoints(this WebApplication app)
    {

        app.MapGet("/api/budget-limits", async (
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
                        category_id,
                        amount,
                        period
                    FROM budget_limits
                    WHERE user_id = @userId
                    ORDER BY period DESC, id DESC;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                var budgetLimits = new List<BudgetLimitResponse>();

                while (await reader.ReadAsync())
                {
                    budgetLimits.Add(new BudgetLimitResponse(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetInt32(2),
                        reader.GetDecimal(3),
                        DateOnly.FromDateTime(reader.GetDateTime(4))
                    ));
                }

                return Results.Ok(budgetLimits);
            }
            catch
            {
                return Results.Problem(
                    "Bütçe limitleri getirilirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapPost("/api/budget-limits", async (
            BudgetLimitRequest budget,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (budget.CategoryId <= 0 || budget.Amount <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Kategori ID ve bütçe tutarı 0'dan büyük olmalıdır"
                });
            }

            if (budget.Period.Day != 1)
            {
                return Results.BadRequest(new
                {
                    message = "Period ayın ilk günü olmalıdır"
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
                    INSERT INTO budget_limits
                        (user_id, category_id, amount, period)
                    VALUES
                        (@userId, @categoryId, @amount, @period)
                    RETURNING id;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("categoryId", budget.CategoryId);
                command.Parameters.AddWithValue("amount", budget.Amount);
                command.Parameters.AddWithValue("period", budget.Period);

                var newId =
                    (int)(await command.ExecuteScalarAsync())!;

                return Results.Created(
                    $"/api/budget-limits/{newId}",
                    new
                    {
                        id = newId,
                        message = "Bütçe limiti başarıyla oluşturuldu"
                    }
                );
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen CategoryId geçerli değil"
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Results.BadRequest(new
                {
                    message = "Bu kategori için bu dönemde zaten bir bütçe limiti var"
                });
            }
            catch
            {
                return Results.Problem(
                    "Bütçe limiti oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapPut("/api/budget-limits/{id}", async (
            int id,
            BudgetLimitRequest budget,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Geçerli bir bütçe limiti ID değeri gönderilmelidir"
                });
            }

            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (budget.CategoryId <= 0 || budget.Amount <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Kategori ID ve bütçe tutarı 0'dan büyük olmalıdır"
                });
            }

            if (budget.Period.Day != 1)
            {
                return Results.BadRequest(new
                {
                    message = "Period ayın ilk günü olmalıdır"
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
                    UPDATE budget_limits
                    SET
                        category_id = @categoryId,
                        amount = @amount,
                        period = @period
                    WHERE id = @id
                    AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("categoryId", budget.CategoryId);
                command.Parameters.AddWithValue("amount", budget.Amount);
                command.Parameters.AddWithValue("period", budget.Period);

                var affectedRows =
                    await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Bütçe limiti bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Bütçe limiti başarıyla güncellendi"
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen CategoryId geçerli değil"
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Results.BadRequest(new
                {
                    message = "Bu kategori için bu dönemde zaten bir bütçe limiti var"
                });
            }
            catch
            {
                return Results.Problem(
                    "Bütçe limiti güncellenirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapDelete("/api/budget-limits/{id}", async (
            int id,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Geçerli bir bütçe limiti ID değeri gönderilmelidir"
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
                    DELETE FROM budget_limits
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
                        message = "Bütçe limiti bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Bütçe limiti başarıyla silindi"
                });
            }
            catch
            {
                return Results.Problem(
                    "Bütçe limiti silinirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();
    }
}