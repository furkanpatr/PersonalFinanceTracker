using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PersonalFinanceTracker.Api.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var configuration = config.Build();

            var connectionString =
                configuration.GetConnectionString("DefaultConnection");

            var connectionBuilder =
                new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Database = "personal_finance_tracker_test"
                };

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    connectionBuilder.ConnectionString
            });
        });
    }
}