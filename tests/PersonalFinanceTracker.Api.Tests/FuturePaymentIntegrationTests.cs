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

    [Fact]
    public async Task PayFuturePayment_Twice_ReturnsBadRequestOnSecondAttempt()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            new
            {
                categoryId = 1,
                amount = 500,
                plannedDate = "2026-12-15",
                importanceLevel = "medium",
                paymentMethodId = 1
            }
        );

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var json =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = json.GetProperty("id").GetInt32();

        var firstResponse =
            await client.PatchAsync($"/api/future-payments/{id}/pay", null);

        var secondResponse =
            await client.PatchAsync($"/api/future-payments/{id}/pay", null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }


    [Fact]
    public async Task PayFuturePayment_ConcurrentRequests_OnlyOneSucceeds()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            new
            {
                categoryId = 1,
                amount = 500,
                plannedDate = "2026-12-15",
                importanceLevel = "medium",
                paymentMethodId = 1
            }
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var json =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = json.GetProperty("id").GetInt32();

        var url = $"/api/future-payments/{id}/pay";

        var firstTask = client.PatchAsync(url, null);
        var secondTask = client.PatchAsync(url, null);

        var responses = await Task.WhenAll(
            firstTask,
            secondTask
        );

        Assert.Single(
            responses,
            r => r.StatusCode == HttpStatusCode.OK
        );

        Assert.Single(
            responses,
            r => r.StatusCode == HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task PostponeFuturePayment_AfterPayment_ReturnsBadRequest()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var plannedDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(30));

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            new
            {
                categoryId = 1,
                amount = 500,
                plannedDate,
                importanceLevel = "medium",
                paymentMethodId = 1
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var json =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = json.GetProperty("id").GetInt32();

        var payResponse =
            await client.PatchAsync($"/api/future-payments/{id}/pay", null);

        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);

        var postponeResponse = await client.PatchAsJsonAsync(
            $"/api/future-payments/{id}/postpone",
            new
            {
                newPlannedDate = plannedDate.AddDays(7)
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            postponeResponse.StatusCode);
    }

    [Fact]
    public async Task PayAndPostpone_ConcurrentRequests_RemainConsistent()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var plannedDate =
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var newPlannedDate = plannedDate.AddDays(7);

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            new
            {
                categoryId = 1,
                amount = 500,
                plannedDate,
                importanceLevel = "medium",
                paymentMethodId = 1
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var json =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = json.GetProperty("id").GetInt32();

        var payTask =
            client.PatchAsync($"/api/future-payments/{id}/pay", null);

        var postponeTask = client.PatchAsJsonAsync(
            $"/api/future-payments/{id}/postpone",
            new { newPlannedDate });

        await Task.WhenAll(payTask, postponeTask);

        Assert.Equal(HttpStatusCode.OK, payTask.Result.StatusCode);

        Assert.Contains(
            postponeTask.Result.StatusCode,
            new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest });

        var getResponse =
            await client.GetAsync($"/api/future-payments/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var payment =
            await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("paid", payment.GetProperty("status").GetString());

        var expectedDate =
            postponeTask.Result.StatusCode == HttpStatusCode.OK
                ? newPlannedDate
                : plannedDate;

        Assert.Equal(
            expectedDate.ToString("yyyy-MM-dd"),
            payment.GetProperty("plannedDate").GetString());
    }

}
