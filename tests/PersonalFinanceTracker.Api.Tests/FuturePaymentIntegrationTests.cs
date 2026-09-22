using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class FuturePaymentIntegrationTests
{
    [Fact]
    public async Task CreateFuturePayment_WithValidData_ReturnsCreated()
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
            amount = 500,
            plannedDate = "2026-10-15",
            importanceLevel = "medium",
            paymentMethodId = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/future-payments",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );
    }

    [Fact]
    public async Task CreateFuturePayment_WithInvalidImportance_ReturnsBadRequest()
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
            amount = 500,
            plannedDate = "2026-10-15",
            importanceLevel = "urgent",
            paymentMethodId = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/future-payments",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task GetFuturePayment_BelongingToAnotherUser_ReturnsNotFound()
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
            amount = 500,
            plannedDate = "2026-10-15",
            importanceLevel = "medium",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            createRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createdJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var futurePaymentId =
            createdJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.GetAsync(
            $"/api/future-payments/{futurePaymentId}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

}