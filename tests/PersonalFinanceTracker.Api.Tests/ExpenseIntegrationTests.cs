using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class ExpenseIntegrationTests
{
    [Fact]
    public async Task CreateExpense_WithValidData_ReturnsCreated()
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
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Integration test expense",
            paymentMethodId = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/expenses",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateExpense_WithNegativeAmount_ReturnsBadRequest()
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
            amount = -250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Invalid amount test",
            paymentMethodId = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/expenses",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateExpense_WithEmptyPlace_ReturnsBadRequest()
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
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "",
            description = "Empty place test",
            paymentMethodId = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/expenses",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateExpense_WithInvalidCategoryId_ReturnsBadRequest()
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
            categoryId = 999999,
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Invalid category test",
            paymentMethodId = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/expenses",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task GetExpense_BelongingToAnotherUser_ReturnsNotFound()
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
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "User scoping test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            createRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var expenseId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.GetAsync(
            $"/api/expenses/{expenseId}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task DeleteExpense_BelongingToAnotherUser_ReturnsNotFound()
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
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Delete user-scoping test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var expenseId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.DeleteAsync(
            $"/api/expenses/{expenseId}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task UpdateExpense_BelongingToAnotherUser_ReturnsNotFound()
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
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Update user-scoping test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var expenseId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var updateRequest = new
        {
            categoryId = 1,
            amount = 750,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Updated Market",
            description = "Unauthorized update test",
            paymentMethodId = 1
        };

        var response = await client.PutAsJsonAsync(
            $"/api/expenses/{expenseId}",
            updateRequest
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task UpdateExpense_WithValidData_ReturnsOk()
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
            categoryId = 1,
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Before update",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var expenseId =
            createJson.GetProperty("id").GetInt32();

        var updateRequest = new
        {
            categoryId = 1,
            amount = 750,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Updated Market",
            description = "After update",
            paymentMethodId = 1
        };

        var response = await client.PutAsJsonAsync(
            $"/api/expenses/{expenseId}",
            updateRequest
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );
    }

    [Fact]
    public async Task DeleteExpense_BelongingToCurrentUser_ReturnsOk()
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
            categoryId = 1,
            amount = 250,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Delete test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var expenseId =
            createJson.GetProperty("id").GetInt32();

        var response = await client.DeleteAsync(
            $"/api/expenses/{expenseId}"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );
    }
}