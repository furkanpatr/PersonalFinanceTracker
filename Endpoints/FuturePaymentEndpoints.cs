using Npgsql;
using PersonalFinanceTracker.Api.Models;
using System.Security.Claims;

namespace PersonalFinanceTracker.Api.Endpoints;

public static class FuturePaymentEndpoints
{
    public static void MapFuturePaymentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/future-payments", async (
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
                        planned_date,
                        status,
                        importance_level,
                        payment_method_id
                    FROM future_payments
                    WHERE user_id = @userId
                    ORDER BY planned_date;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                var futurePayments =
                    new List<FuturePaymentResponse>();

                while (await reader.ReadAsync())
                {
                    futurePayments.Add(
                        new FuturePaymentResponse(
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetInt32(2),
                            reader.GetDecimal(3),
                            DateOnly.FromDateTime(reader.GetDateTime(4)),
                            reader.GetString(5),
                            reader.IsDBNull(6) ? null : reader.GetString(6),
                            reader.GetInt32(7)
                        )
                    );
                }

                return Results.Ok(futurePayments);
            }
            catch
            {
                return Results.Problem(
                    "Gelecek ödemeler getirilirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapGet("/api/future-payments/{id}", async (
            int id,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id 0'dan büyük olmalıdır"
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
                    SELECT
                        id,
                        user_id,
                        category_id,
                        amount,
                        planned_date,
                        status,
                        importance_level,
                        payment_method_id
                    FROM future_payments
                    WHERE id = @id
                      AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Results.NotFound(new
                    {
                        message = "Gelecek ödeme bulunamadı"
                    });
                }

                var futurePayment = new FuturePaymentResponse(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetDecimal(3),
                    DateOnly.FromDateTime(reader.GetDateTime(4)),
                    reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetInt32(7)
                );

                return Results.Ok(futurePayment);
            }
            catch
            {
                return Results.Problem(
                    "Gelecek ödeme getirilirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapPost("/api/future-payments", async (
            FuturePaymentRequest request,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (request.Amount <= 0 ||
                request.CategoryId <= 0 ||
                request.PaymentMethodId <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Tutar, CategoryId ve PaymentMethodId 0'dan büyük olmalıdır"
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
                    INSERT INTO future_payments
                        (user_id, category_id, amount, planned_date, status, importance_level, payment_method_id)
                    VALUES
                        (@userId, @categoryId, @amount, @plannedDate, @status, @importanceLevel, @paymentMethodId)
                    RETURNING id;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("categoryId", request.CategoryId);
                command.Parameters.AddWithValue("amount", request.Amount);
                command.Parameters.AddWithValue("plannedDate", request.PlannedDate);
                command.Parameters.AddWithValue("status", request.Status);
                command.Parameters.AddWithValue(
                    "importanceLevel",
                    (object?)request.ImportanceLevel ?? DBNull.Value
                );
                command.Parameters.AddWithValue(
                    "paymentMethodId",
                    request.PaymentMethodId
                );

                var newId =
                    Convert.ToInt32(await command.ExecuteScalarAsync());

                return Results.Created(
                    $"/api/future-payments/{newId}",
                    new
                    {
                        id = newId,
                        message = "Gelecek ödeme başarıyla oluşturuldu"
                    }
                );
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen CategoryId veya PaymentMethodId geçerli değil"
                });
            }
            catch
            {
                return Results.Problem(
                    "Gelecek ödeme eklenirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapPut("/api/future-payments/{id}", async (
            int id,
            FuturePaymentRequest request,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id 0'dan büyük olmalıdır"
                });
            }

            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (request.Amount <= 0 ||
                request.CategoryId <= 0 ||
                request.PaymentMethodId <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Tutar, CategoryId ve PaymentMethodId 0'dan büyük olmalıdır"
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
                    UPDATE future_payments
                    SET
                        category_id = @categoryId,
                        amount = @amount,
                        planned_date = @plannedDate,
                        status = @status,
                        importance_level = @importanceLevel,
                        payment_method_id = @paymentMethodId
                    WHERE id = @id
                      AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("categoryId", request.CategoryId);
                command.Parameters.AddWithValue("amount", request.Amount);
                command.Parameters.AddWithValue("plannedDate", request.PlannedDate);
                command.Parameters.AddWithValue("status", request.Status);
                command.Parameters.AddWithValue(
                    "importanceLevel",
                    (object?)request.ImportanceLevel ?? DBNull.Value
                );
                command.Parameters.AddWithValue(
                    "paymentMethodId",
                    request.PaymentMethodId
                );

                var affectedRows =
                    await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Gelecek ödeme bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Gelecek ödeme başarıyla güncellendi"
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen CategoryId veya PaymentMethodId geçerli değil"
                });
            }
            catch
            {
                return Results.Problem(
                    "Gelecek ödeme güncellenirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapDelete("/api/future-payments/{id}", async (
            int id,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id 0'dan büyük olmalıdır"
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
                    DELETE FROM future_payments
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
                        message = "Gelecek ödeme bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Gelecek ödeme başarıyla silindi"
                });
            }
            catch
            {
                return Results.Problem(
                    "Gelecek ödeme silinirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();
    }
}