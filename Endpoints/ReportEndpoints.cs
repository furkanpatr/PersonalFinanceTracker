using Npgsql;
using System.Security.Claims;

namespace PersonalFinanceTracker.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/api/reports/summary", async (
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
                        COALESCE(SUM(amount), 0),
                        COUNT(*),
                        COALESCE(AVG(amount), 0),
                        COALESCE(MAX(amount), 0),
                        COALESCE(MIN(amount), 0)
                    FROM expenses
                    WHERE user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                decimal totalExpense;
                long expenseCount;
                decimal averageExpense;
                decimal highestExpense;
                decimal lowestExpense;

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();

                    totalExpense = reader.GetDecimal(0);
                    expenseCount = reader.GetInt64(1);
                    averageExpense = reader.GetDecimal(2);
                    highestExpense = reader.GetDecimal(3);
                    lowestExpense = reader.GetDecimal(4);
                }

                var futurePaymentsSql = """
                    SELECT
                        COALESCE(SUM(amount), 0),
                        COALESCE(
                            SUM(amount) FILTER (WHERE importance_level = 'Yüksek'),
                            0
                        )
                    FROM future_payments
                    WHERE user_id = @userId
                        AND status = 'Bekliyor';
                    """;

                await using var futurePaymentsCommand =
                    new NpgsqlCommand(futurePaymentsSql, connection);

                futurePaymentsCommand.Parameters.AddWithValue("userId", userId);

                await using var futurePaymentsReader =
                    await futurePaymentsCommand.ExecuteReaderAsync();

                await futurePaymentsReader.ReadAsync();

                var upcomingPaymentsTotal =
                    futurePaymentsReader.GetDecimal(0);

                var highImportanceUpcomingTotal =
                    futurePaymentsReader.GetDecimal(1);

                return Results.Ok(new
                {
                    totalExpense,
                    expenseCount,
                    averageExpense,
                    highestExpense,
                    lowestExpense,
                    upcomingPaymentsTotal,
                    highImportanceUpcomingTotal
                });

            }
            catch
            {
                return Results.Problem(
                    "Rapor oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapGet("/api/reports/categories", async (
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
                        categories.name,
                        COALESCE(SUM(expenses.amount), 0),
                        COUNT(expenses.id),
                        COALESCE(AVG(expenses.amount), 0)
                    FROM expenses
                    JOIN categories
                        ON expenses.category_id = categories.id
                    WHERE expenses.user_id = @userId
                    GROUP BY categories.name
                    ORDER BY SUM(expenses.amount) DESC;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                var categories = new List<object>();

                while (await reader.ReadAsync())
                {
                    categories.Add(new
                    {
                        categoryName = reader.GetString(0),
                        totalExpense = reader.GetDecimal(1),
                        expenseCount = reader.GetInt64(2),
                        averageExpense = reader.GetDecimal(3)
                    });
                }

                return Results.Ok(categories);
            }
            catch
            {
                return Results.Problem(
                    "Kategori raporu oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapGet("/api/reports/monthly", async (
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
                        COALESCE(
                            SUM(amount) FILTER (
                                WHERE date >= DATE_TRUNC('month', CURRENT_DATE)
                                    AND date < DATE_TRUNC('month', CURRENT_DATE) + INTERVAL '1 month'
                            ),
                            0
                        ),
                        COALESCE(
                            SUM(amount) FILTER (
                                WHERE date >= DATE_TRUNC('month', CURRENT_DATE) - INTERVAL '1 month'
                                    AND date < DATE_TRUNC('month', CURRENT_DATE)
                            ),
                            0
                        )
                    FROM expenses
                    WHERE user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                await reader.ReadAsync();

                var currentMonthTotal =
                    reader.GetDecimal(0);

                var previousMonthTotal =
                    reader.GetDecimal(1);

                var difference =
                    currentMonthTotal - previousMonthTotal;

                decimal percentageChange = 0;

                if (previousMonthTotal > 0)
                {
                    percentageChange =
                        (difference / previousMonthTotal) * 100;
                }

                return Results.Ok(new
                {
                    currentMonthTotal,
                    previousMonthTotal,
                    difference,
                    percentageChange
                });
            }
            catch
            {
                return Results.Problem(
                    "Aylık rapor oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapGet("/api/reports/future-payments/status", async (
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
                        status,
                        COUNT(*),
                        COALESCE(SUM(amount), 0)
                    FROM future_payments
                    WHERE user_id = @userId
                    GROUP BY status
                    ORDER BY status;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                var statuses = new List<object>();

                while (await reader.ReadAsync())
                {
                    statuses.Add(new
                    {
                        status = reader.GetString(0),
                        paymentCount = reader.GetInt64(1),
                        totalAmount = reader.GetDecimal(2)
                    });
                }

                return Results.Ok(statuses);
            }
            catch
            {
                return Results.Problem(
                    "Gelecek ödemeler durum raporu oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapGet("/api/reports/weekly", async (
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
                        COALESCE(
                            SUM(amount) FILTER (
                                WHERE date >= DATE_TRUNC('week', CURRENT_DATE)
                                    AND date < DATE_TRUNC('week', CURRENT_DATE) + INTERVAL '1 week'
                            ),
                            0
                        ),
                        COALESCE(
                            SUM(amount) FILTER (
                                WHERE date >= DATE_TRUNC('week', CURRENT_DATE) - INTERVAL '1 week'
                                    AND date < DATE_TRUNC('week', CURRENT_DATE)
                            ),
                            0
                        )
                    FROM expenses
                    WHERE user_id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);

                await using var reader =
                    await command.ExecuteReaderAsync();

                await reader.ReadAsync();

                var currentWeekTotal =
                    reader.GetDecimal(0);

                var previousWeekTotal =
                    reader.GetDecimal(1);

                var difference =
                    currentWeekTotal - previousWeekTotal;

                decimal percentageChange = 0;

                if (previousWeekTotal > 0)
                {
                    percentageChange =
                        (difference / previousWeekTotal) * 100;
                }

                return Results.Ok(new
                {
                    currentWeekTotal,
                    previousWeekTotal,
                    difference,
                    percentageChange
                });
            }
            catch
            {
                return Results.Problem(
                    "Haftalık rapor oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();

        app.MapGet("/api/reports/range", async (
            DateOnly startDate,
            DateOnly endDate,
            IConfiguration configuration,
            ClaimsPrincipal user) =>
        {
            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Results.Unauthorized();
            }

            if (startDate > endDate)
            {
                return Results.BadRequest(new
                {
                    message = "Başlangıç tarihi bitiş tarihinden büyük olamaz"
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
                        COALESCE(SUM(amount), 0),
                        COUNT(*),
                        COALESCE(AVG(amount), 0),
                        COALESCE(MAX(amount), 0),
                        COALESCE(MIN(amount), 0)
                    FROM expenses
                    WHERE user_id = @userId
                        AND date BETWEEN @startDate AND @endDate;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("startDate", startDate);
                command.Parameters.AddWithValue("endDate", endDate);

                await using var reader =
                    await command.ExecuteReaderAsync();

                await reader.ReadAsync();

                var totalExpense = reader.GetDecimal(0);
                var expenseCount = reader.GetInt64(1);
                var averageExpense = reader.GetDecimal(2);
                var highestExpense = reader.GetDecimal(3);
                var lowestExpense = reader.GetDecimal(4);

                return Results.Ok(new
                {
                    startDate,
                    endDate,
                    totalExpense,
                    expenseCount,
                    averageExpense,
                    highestExpense,
                    lowestExpense
                });
            }
            catch
            {
                return Results.Problem(
                    "Tarih aralığı raporu oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        })
        .RequireAuthorization();
    }
}