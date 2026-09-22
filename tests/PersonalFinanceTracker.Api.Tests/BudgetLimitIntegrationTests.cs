using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class BudgetLimitIntegrationTests
{
    [Fact]
    public async Task CreateBudgetLimit_WithValidData_ReturnsCreated()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            categoryId = 1,
            amount = 5000,
            period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1)
        };

        var response = await client.PostAsJsonAsync(
            "/api/budget-limits",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateBudgetLimit_WithInvalidPeriod_ReturnsBadRequest()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            categoryId = 1,
            amount = 5000,
            period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 15)
        };

        var response = await client.PostAsJsonAsync(
            "/api/budget-limits",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateBudgetLimit_WithDuplicateCategoryAndPeriod_ReturnsBadRequest()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var period =
            new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

        var request = new
        {
            categoryId = 1,
            amount = 5000,
            period
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/budget-limits",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode
        );

        var secondResponse = await client.PostAsJsonAsync(
            "/api/budget-limits",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode
        );
    }

    [Fact]
    public async Task CreateBudgetLimit_WithNegativeAmount_ReturnsBadRequest()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            categoryId = 1,
            amount = -500,
            period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1)
        };

        var response = await client.PostAsJsonAsync(
            "/api/budget-limits",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task DeleteBudgetLimit_BelongingToAnotherUser_ReturnsNotFound()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var userAToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);

        var request = new
        {
            categoryId = 1,
            amount = 5000,
            period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1)
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/budget-limits",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var budgetLimitId =
            createJson.GetProperty("id").GetInt32();

        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.DeleteAsync(
            $"/api/budget-limits/{budgetLimitId}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task UpdateBudgetLimit_BelongingToAnotherUser_ReturnsNotFound()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // User A
        var userAToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);

        var createRequest = new
        {
            categoryId = 1,
            amount = 5000,
            period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1)
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/budget-limits",
            createRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var budgetLimitId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var updateRequest = new
        {
            categoryId = 1,
            amount = 7500,
            period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1)
        };

        var response = await client.PutAsJsonAsync(
            $"/api/budget-limits/{budgetLimitId}",
            updateRequest
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task GetBudgetLimits_DoesNotReturnAnotherUsersBudget()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // User A
        var userAToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);

        var createRequest = new
        {
            categoryId = 1,
            amount = 5000,
            period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1)
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/budget-limits",
            createRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.GetAsync(
            "/api/budget-limits"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, json.GetArrayLength());
    }
}