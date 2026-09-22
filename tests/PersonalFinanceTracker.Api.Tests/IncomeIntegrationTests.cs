using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class IncomeIntegrationTests
{
    [Fact]
    public async Task CreateIncome_WithValidData_ReturnsCreated()
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
            incomeCategoryId = 1,
            amount = 5000,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Integration test income"
        };

        var response = await client.PostAsJsonAsync(
            "/api/incomes",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateIncome_WithNegativeAmount_ReturnsBadRequest()
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
            incomeCategoryId = 1,
            amount = -500,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Invalid income amount test"
        };

        var response = await client.PostAsJsonAsync(
            "/api/incomes",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateIncome_WithInvalidCategoryId_ReturnsBadRequest()
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
            incomeCategoryId = 999999,
            amount = 5000,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Invalid income category test"
        };

        var response = await client.PostAsJsonAsync(
            "/api/incomes",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task GetIncomes_DoesNotReturnAnotherUsersIncome()
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
            incomeCategoryId = 1,
            amount = 5000,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "User scoping income test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/incomes",
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
            "/api/incomes"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, json.GetArrayLength());
    }

    [Fact]
    public async Task DeleteIncome_BelongingToAnotherUser_ReturnsNotFound()
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
            incomeCategoryId = 1,
            amount = 5000,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Delete user-scoping income test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/incomes",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var incomeId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.DeleteAsync(
            $"/api/incomes/{incomeId}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task UpdateIncome_BelongingToAnotherUser_ReturnsNotFound()
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
            incomeCategoryId = 1,
            amount = 5000,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Update user-scoping income test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/incomes",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var incomeId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var updateRequest = new
        {
            incomeCategoryId = 1,
            amount = 7500,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Unauthorized income update"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/incomes/{incomeId}",
            updateRequest
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task UpdateIncome_WithValidData_ReturnsOk()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            incomeCategoryId = 1,
            amount = 5000,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Before update"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/incomes",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var incomeId =
            createJson.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            incomeCategoryId = 1,
            amount = 7500,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "After update"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/incomes/{incomeId}",
            updateRequest
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );
    }

    [Fact]
    public async Task DeleteIncome_BelongingToCurrentUser_ReturnsOk()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            incomeCategoryId = 1,
            amount = 5000,
            date = DateOnly.FromDateTime(DateTime.Today),
            description = "Delete income test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/incomes",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var incomeId =
            createJson.GetProperty("id").GetInt32();

        var response = await client.DeleteAsync(
            $"/api/incomes/{incomeId}"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );
    }

}