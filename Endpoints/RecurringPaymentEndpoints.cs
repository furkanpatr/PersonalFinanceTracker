using Npgsql;
using PersonalFinanceTracker.Api.Models;
using System.Security.Claims;

namespace PersonalFinanceTracker.Api.Endpoints;

public static class RecurringPaymentEndpoints
{
    public static void MapRecurringPaymentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/recurring-payments", async (
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                await using var connection = new NpgsqlConnection(
                    configuration.GetConnectionString("DefaultConnection"));

                await connection.OpenAsync();

                var sql = """
                    SELECT
                        id,
                        category_id,
                        payment_method_id,
                        name,
                        amount,
                        frequency,
                        next_due_date,
                        importance_level,
                        is_active,
                        description
                    FROM recurring_payments
                    WHERE user_id = @userId
                    ORDER BY id;
                    """;

                await using var command = new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader = await command.ExecuteReaderAsync();

                var recurringPayments = new List<RecurringPaymentResponse>();

                while (await reader.ReadAsync())
                {
                    recurringPayments.Add(new RecurringPaymentResponse(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetInt32(2),
                        reader.GetString(3),
                        reader.GetDecimal(4),
                        reader.GetString(5),
                        reader.GetFieldValue<DateOnly>(6),
                        reader.GetString(7),
                        reader.GetBoolean(8),
                        reader.IsDBNull(9) ? null : reader.GetString(9)
                    ));
                }

                return Results.Ok(recurringPayments);
            }
            catch (Exception)
            {
                return Results.Problem(
                    "Recurring payments alınırken bir hata oluştu.");
            }
        })
        .RequireAuthorization();

        app.MapPost("/api/recurring-payments", async (
            RecurringPaymentRequest recurringPayment,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (recurringPayment.CategoryId <= 0 ||
                recurringPayment.PaymentMethodId <= 0 ||
                recurringPayment.Amount <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Kategori ID, ödeme yöntemi ID ve tutar 0'dan büyük olmalıdır"
                });
            }

            if (string.IsNullOrWhiteSpace(recurringPayment.Name))
            {
                return Results.BadRequest(new
                {
                    message = "Ödeme adı boş olamaz"
                });
            }

            if (recurringPayment.Frequency is not ("weekly" or "monthly" or "yearly"))
            {
                return Results.BadRequest(new
                {
                    message = "Frequency weekly, monthly veya yearly olmalıdır"
                });
            }

            if (recurringPayment.ImportanceLevel is not ("low" or "medium" or "high"))
            {
                return Results.BadRequest(new
                {
                    message = "ImportanceLevel low, medium veya high olmalıdır"
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
                    INSERT INTO recurring_payments
                        (user_id, category_id, payment_method_id, name, amount,
                        frequency, next_due_date, importance_level, description)
                    VALUES
                        (@userId, @categoryId, @paymentMethodId, @name, @amount,
                        @frequency, @nextDueDate, @importanceLevel, @description)
                    RETURNING id;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue(
                    "categoryId", recurringPayment.CategoryId);
                command.Parameters.AddWithValue(
                    "paymentMethodId", recurringPayment.PaymentMethodId);
                command.Parameters.AddWithValue("name", recurringPayment.Name);
                command.Parameters.AddWithValue("amount", recurringPayment.Amount);
                command.Parameters.AddWithValue("frequency", recurringPayment.Frequency);
                command.Parameters.AddWithValue(
                    "nextDueDate", recurringPayment.NextDueDate);
                command.Parameters.AddWithValue(
                    "importanceLevel", recurringPayment.ImportanceLevel);
                command.Parameters.AddWithValue(
                    "description",
                    (object?)recurringPayment.Description ?? DBNull.Value);

                var newId =
                    (int)(await command.ExecuteScalarAsync())!;

                return Results.Created(
                    $"/api/recurring-payments/{newId}",
                    new
                    {
                        id = newId,
                        message = "Tekrarlayan ödeme başarıyla oluşturuldu"
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
                    "Tekrarlayan ödeme oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapPut("/api/recurring-payments/{id:int}", async (
            int id,
            RecurringPaymentRequest recurringPayment,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id geçerli olmalıdır"
                });
            }

            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (recurringPayment.CategoryId <= 0 ||
                recurringPayment.PaymentMethodId <= 0 ||
                recurringPayment.Amount <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Kategori ID, ödeme yöntemi ID ve tutar 0'dan büyük olmalıdır"
                });
            }

            if (string.IsNullOrWhiteSpace(recurringPayment.Name))
            {
                return Results.BadRequest(new
                {
                    message = "Ödeme adı boş olamaz"
                });
            }

            if (recurringPayment.Frequency is not ("weekly" or "monthly" or "yearly"))
            {
                return Results.BadRequest(new
                {
                    message = "Frequency weekly, monthly veya yearly olmalıdır"
                });
            }

            if (recurringPayment.ImportanceLevel is not ("low" or "medium" or "high"))
            {
                return Results.BadRequest(new
                {
                    message = "ImportanceLevel low, medium veya high olmalıdır"
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
                    UPDATE recurring_payments
                    SET
                        category_id = @categoryId,
                        payment_method_id = @paymentMethodId,
                        name = @name,
                        amount = @amount,
                        frequency = @frequency,
                        next_due_date = @nextDueDate,
                        importance_level = @importanceLevel,
                        description = @description
                    WHERE id = @id
                    AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("categoryId", recurringPayment.CategoryId);
                command.Parameters.AddWithValue("paymentMethodId", recurringPayment.PaymentMethodId);
                command.Parameters.AddWithValue("name", recurringPayment.Name);
                command.Parameters.AddWithValue("amount", recurringPayment.Amount);
                command.Parameters.AddWithValue("frequency", recurringPayment.Frequency);
                command.Parameters.AddWithValue("nextDueDate", recurringPayment.NextDueDate);
                command.Parameters.AddWithValue("importanceLevel", recurringPayment.ImportanceLevel);
                command.Parameters.AddWithValue(
                    "description",
                    (object?)recurringPayment.Description ?? DBNull.Value);

                var affectedRows = await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Tekrarlayan ödeme bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Tekrarlayan ödeme başarıyla güncellendi"
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
                    "Tekrarlayan ödeme güncellenirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapDelete("/api/recurring-payments/{id:int}", async (
            int id,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id geçerli olmalıdır"
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
                    DELETE FROM recurring_payments
                    WHERE id = @id
                    AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);

                var affectedRows = await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Tekrarlayan ödeme bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Tekrarlayan ödeme başarıyla silindi"
                });
            }
            catch
            {
                return Results.Problem(
                    "Tekrarlayan ödeme silinirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapPatch("/api/recurring-payments/{id:int}/status", async (
            int id,
            RecurringPaymentStatusRequest request,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id geçerli olmalıdır"
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
                    UPDATE recurring_payments
                    SET is_active = @isActive
                    WHERE id = @id
                    AND user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("isActive", request.IsActive);
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", userId);

                var affectedRows = await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Tekrarlayan ödeme bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Tekrarlayan ödeme durumu başarıyla güncellendi"
                });
            }
            catch
            {
                return Results.Problem(
                    "Tekrarlayan ödeme durumu güncellenirken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapPost("/api/recurring-payments/process-due", async (
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

                await using var transaction =
                    await connection.BeginTransactionAsync();

                var selectDueSql = """
                    SELECT
                        id,
                        category_id,
                        amount,
                        next_due_date,
                        importance_level,
                        payment_method_id,
                        frequency
                    FROM recurring_payments
                    WHERE user_id = @userId
                    AND is_active = TRUE
                    AND next_due_date <= CURRENT_DATE
                    ORDER BY next_due_date, id;
                    """;

                var duePayments = new List<(
                    int Id,
                    int CategoryId,
                    decimal Amount,
                    DateOnly NextDueDate,
                    string ImportanceLevel,
                    int PaymentMethodId,
                    string Frequency
                )>();

                await using (var selectCommand =
                    new NpgsqlCommand(selectDueSql, connection, transaction))
                {
                    selectCommand.Parameters.AddWithValue("userId", userId);

                    await using var reader =
                        await selectCommand.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        duePayments.Add((
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetDecimal(2),
                            reader.GetFieldValue<DateOnly>(3),
                            reader.GetString(4),
                            reader.GetInt32(5),
                            reader.GetString(6)
                        ));
                    }
                }

                var createdCount = 0;

                foreach (var payment in duePayments)
                {
                    var insertSql = """
                        INSERT INTO future_payments
                            (user_id, category_id, amount, planned_date, original_due_date,
                            importance_level, payment_method_id, recurring_payment_id)
                        VALUES
                            (@userId, @categoryId, @amount, @plannedDate, @originalDueDate,
                            @importanceLevel, @paymentMethodId, @recurringPaymentId)
                        ON CONFLICT (recurring_payment_id, original_due_date)
                        WHERE recurring_payment_id IS NOT NULL
                        DO NOTHING;
                        """;

                    await using var insertCommand =
                        new NpgsqlCommand(insertSql, connection, transaction);

                    insertCommand.Parameters.AddWithValue("userId", userId);
                    insertCommand.Parameters.AddWithValue("categoryId", payment.CategoryId);
                    insertCommand.Parameters.AddWithValue("amount", payment.Amount);
                    insertCommand.Parameters.AddWithValue("plannedDate", payment.NextDueDate);
                    insertCommand.Parameters.AddWithValue("importanceLevel", payment.ImportanceLevel);
                    insertCommand.Parameters.AddWithValue("paymentMethodId", payment.PaymentMethodId);
                    insertCommand.Parameters.AddWithValue("recurringPaymentId", payment.Id);
                    insertCommand.Parameters.AddWithValue("originalDueDate", payment.NextDueDate);

                    var insertedRows = await insertCommand.ExecuteNonQueryAsync();

                    if (insertedRows > 0)
                    {
                        createdCount++;
                    }

                    var newNextDueDate = payment.Frequency switch
                    {
                        "weekly" => payment.NextDueDate.AddDays(7),
                        "monthly" => payment.NextDueDate.AddMonths(1),
                        "yearly" => payment.NextDueDate.AddYears(1),
                        _ => throw new InvalidOperationException("Geçersiz frequency")
                    };

                    var updateSql = """
                        UPDATE recurring_payments
                        SET next_due_date = @newNextDueDate
                        WHERE id = @id
                        AND user_id = @userId;
                        """;

                    await using var updateCommand =
                        new NpgsqlCommand(updateSql, connection, transaction);

                    updateCommand.Parameters.AddWithValue("newNextDueDate", newNextDueDate);
                    updateCommand.Parameters.AddWithValue("id", payment.Id);
                    updateCommand.Parameters.AddWithValue("userId", userId);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();

                return Results.Ok(new
                {
                    createdCount = createdCount,
                    message = "Vadesi gelen tekrarlayan ödemeler başarıyla işlendi"
                });
            }
            catch
            {
                return Results.Problem(
                    "Vadesi gelen tekrarlayan ödemeler işlenirken bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();
    }
}