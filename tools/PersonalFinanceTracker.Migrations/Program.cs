using DbUp;
using Npgsql;
using System.Reflection;

var connectionString =
    Environment.GetEnvironmentVariable("PFT_MIGRATIONS_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Migration bağlantı bilgisi bulunamadı.");
    return 1;
}

var connectionOptions =
    new NpgsqlConnectionStringBuilder(connectionString);

var databaseName = connectionOptions.Database;

var isLocalHost =
connectionOptions.Host is "localhost" or "127.0.0.1";

var isDockerHost =
connectionOptions.Host == "db" &&
Environment.GetEnvironmentVariable("PFT_ALLOW_DOCKER_DB") == "YES";

if (!isLocalHost && !isDockerHost)
{
    Console.Error.WriteLine("Migration hedef sunucusuna izin verilmedi.");
    return 1;
}

var isTestDatabase =
    databaseName == "personal_finance_tracker_automated_test";

var isOriginalDatabaseAuthorized =
    databaseName == "expense_tracker" &&
    Environment.GetEnvironmentVariable("PFT_ALLOW_ORIGINAL_DB") == "YES";

if (!isTestDatabase && !isOriginalDatabaseAuthorized)
{
    Console.Error.WriteLine("İzin verilmeyen hedef veritabanı.");
    return 1;
}

if (databaseName == "personal_finance_tracker_automated_test")
{
    var adminConnectionString =
        new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        }.ConnectionString;

    using var adminConnection =
        new NpgsqlConnection(adminConnectionString);

    adminConnection.Open();

    using var checkCommand = new NpgsqlCommand(
        "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name)",
        adminConnection
    );

    checkCommand.Parameters.AddWithValue("name", databaseName);

    var databaseExists = (bool)checkCommand.ExecuteScalar()!;

    if (!databaseExists)
    {
        using var createCommand = new NpgsqlCommand(
            $"CREATE DATABASE \"{databaseName}\"",
            adminConnection
        );

        createCommand.ExecuteNonQuery();

        Console.WriteLine("Test veritabani otomatik olusturuldu.");
    }
}

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .WithTransactionPerScript()
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.Error.WriteLine("Migration başarısız oldu.");
    return 1;
}

Console.WriteLine("Migration başarıyla tamamlandı.");
return 0;
