using Npgsql;
using PersonalFinanceTracker.Api.Models;

namespace PersonalFinanceTracker.Api.Endpoints;

// Expense ile ilgili endpoint'leri tek yerde toplar.
public static class ExpenseEndpoints
{

    public static void MapExpenseEndpoints(this WebApplication app)

    {
        // Veritabanındaki tüm harcamaları getiren endpoint
        app.MapGet("/api/expenses", async (IConfiguration configuration) =>
        {
            try
            {
                 var connectionString =
                configuration.GetConnectionString("DefaultConnection");

            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            var sql = """
                SELECT id, user_id, category_id, amount, date, place, description, payment_method_id
                FROM expenses
                ORDER BY id;
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            var expenses = new List<ExpenseResponse>();

            while (await reader.ReadAsync())
            {
                expenses.Add(new ExpenseResponse(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetDecimal(3),
                    DateOnly.FromDateTime(reader.GetDateTime(4)),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetInt32(7)
                ));
            }

            return Results.Ok(expenses);
            }
            catch
            {
                return Results.Problem(
                    "Harcamalar getirilirken beklenmeyen bir hata oluştu."
                );
            }
        });

        // ID ile tek bir harcamayı veritabanından getiren endpoint
        app.MapGet("/api/expenses/{id}", async (int id, IConfiguration configuration) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id 0'dan büyük olmalıdır"
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
                SELECT id, user_id, category_id, amount, date, place, description, payment_method_id
                FROM expenses
                WHERE id = @id;
                """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);

                await using var reader =
                    await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Results.NotFound(new
                    {
                        message = "Harcama bulunamadı"
                    });
                }

                var expense = new ExpenseResponse(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetDecimal(3),
                    DateOnly.FromDateTime(reader.GetDateTime(4)),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetInt32(7)
                );

                return Results.Ok(expense);
            }
            catch
            {
                return Results.Problem(
                    "Harcama getirilirken beklenmeyen bir hata oluştu."
                );
            }
           
        });

        // Yeni harcamayı veritabanına ekleyen endpoint
        app.MapPost("/api/expenses", async (ExpenseRequest expense, IConfiguration configuration) =>
        {
            //Validasyon: Tutar ve ID değerleri 0'dan büyük olmalıdır, yer bilgisi boş olamaz
            if (expense.Amount <= 0 ||
                expense.UserId <= 0 ||
                expense.CategoryId <= 0 ||
                expense.PaymentMethodId <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Tutar ve ID değerleri 0'dan büyük olmalıdır"
                });
            }

            if (string.IsNullOrWhiteSpace(expense.Place))
            {
                return Results.BadRequest(new
                {
                    message = "Yer bilgisi boş olamaz"
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
                INSERT INTO expenses
                (user_id, category_id, amount, date, place, description, payment_method_id)
                VALUES
                (@userId, @categoryId, @amount, @date, @place, @description, @paymentMethodId)
                RETURNING id;
                """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("userId", expense.UserId);
                command.Parameters.AddWithValue("categoryId", expense.CategoryId);
                command.Parameters.AddWithValue("amount", expense.Amount);
                command.Parameters.AddWithValue("date", expense.Date);
                command.Parameters.AddWithValue("place", expense.Place);
                command.Parameters.AddWithValue("description", expense.Description);
                command.Parameters.AddWithValue("paymentMethodId", expense.PaymentMethodId);

                var newId = (int)(await command.ExecuteScalarAsync())!;

                return Results.Created($"/api/expenses/{newId}", new
                {
                    id = newId,
                    message = "Harcama başarıyla eklendi"
                });
            }

            // Foreign key constraint hatası yakalanırsa, kullanıcıya anlamlı bir mesaj döndürülür
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen UserId, CategoryId veya PaymentMethodId geçerli değil"
                });
            }
            // Diğer beklenmeyen hatalar için genel bir hata mesajı döndürülür
            catch
            {
                return Results.Problem(
                    "Harcama eklenirken beklenmeyen bir hata oluştu."
                );
            }

        });

        // ID ile bir harcamayı veritabanında güncelleyen endpoint
        app.MapPut("/api/expenses/{id}", async (int id, ExpenseRequest expense, IConfiguration configuration) =>
        {
            if (expense.Amount <= 0 ||
                expense.UserId <= 0 ||
                expense.CategoryId <= 0 ||
                expense.PaymentMethodId <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Tutar ve ID değerleri 0'dan büyük olmalıdır"
                });
            }

            if (string.IsNullOrWhiteSpace(expense.Place))
            {
                return Results.BadRequest(new
                {
                    message = "Yer bilgisi boş olamaz"
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
                UPDATE expenses
                SET user_id = @userId,
                category_id = @categoryId,
                amount = @amount,
                date = @date,
                place = @place,
                description = @description,
                payment_method_id = @paymentMethodId
                WHERE id = @id;
                """;

                await using var command =
                                new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("userId", expense.UserId);
                command.Parameters.AddWithValue("categoryId", expense.CategoryId);
                command.Parameters.AddWithValue("amount", expense.Amount);
                command.Parameters.AddWithValue("date", expense.Date);
                command.Parameters.AddWithValue("place", expense.Place);
                command.Parameters.AddWithValue("description", expense.Description);
                command.Parameters.AddWithValue("paymentMethodId", expense.PaymentMethodId);

                var affectedRows =
                    await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Harcama bulunamadı"
                    });
                }
                return Results.Ok(new
                {
                    message = "Harcama başarıyla güncellendi"
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                return Results.BadRequest(new
                {
                    message = "Gönderilen UserId, CategoryId veya PaymentMethodId geçerli değil"
                });
            }
            catch
            {
                return Results.Problem(
                    "Harcama güncellenirken beklenmeyen bir hata oluştu."
                );
            }
        });

        // ID ile bir harcamayı veritabanından silen endpoint
        app.MapDelete("/api/expenses/{id}", async (int id, IConfiguration configuration) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Id 0'dan büyük olmalıdır"
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
                DELETE FROM expenses
                WHERE id = @id;
                """;

                await using var command =
                                new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("id", id);

                var affectedRows =
                    await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    return Results.NotFound(new
                    {
                        message = "Harcama bulunamadı"
                    });
                }

                return Results.Ok(new
                {
                    message = "Harcama başarıyla silindi"
                });
            }
            catch
            {
                return Results.Problem(
                    "Harcama silinirken beklenmeyen bir hata oluştu."
                );
            }


        });



    }










}
