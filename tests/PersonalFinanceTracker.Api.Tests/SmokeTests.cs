using System.Net;

namespace PersonalFinanceTracker.Api.Tests;

public class SmokeTests
{
    [Fact]
    public async Task Expenses_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var response =
            await client.GetAsync("/api/expenses");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }
}