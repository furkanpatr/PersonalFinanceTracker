using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class FinancialDisciplineIntegrationTests
{
    [Fact]
    public async Task FinancialDisciplineScore_WithNoData_ReturnsInsufficientData()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            "/api/reports/financial-discipline-score"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Financial discipline score için yeterli veri yok",
            json.GetProperty("message").GetString()
        );
    }

    [Fact]
    public async Task FinancialDisciplineScore_WithHighOverduePayment_Returns55()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var futurePaymentRequest = new
        {
            categoryId = 1,
            amount = 500,
            plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            importanceLevel = "high",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            futurePaymentRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var scoreResponse = await client.GetAsync(
            "/api/reports/financial-discipline-score"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            scoreResponse.StatusCode
        );

        var json =
            await scoreResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(55, json.GetProperty("score").GetInt32());
        Assert.Equal(1, json.GetProperty("overduePayments").GetInt32());
    }

    [Fact]
    public async Task FinancialDisciplineScore_AfterPayingHighOverduePayment_Returns60()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var futurePaymentRequest = new
        {
            categoryId = 1,
            amount = 500,
            plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            importanceLevel = "high",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            futurePaymentRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var futurePaymentId =
            createJson.GetProperty("id").GetInt32();

        var payResponse = await client.PatchAsync(
            $"/api/future-payments/{futurePaymentId}/pay",
            null
        );

        Assert.Equal(
            HttpStatusCode.OK,
            payResponse.StatusCode
        );

        var scoreResponse = await client.GetAsync(
            "/api/reports/financial-discipline-score"
        );

        var json =
            await scoreResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(60, json.GetProperty("score").GetInt32());
        Assert.Equal(1, json.GetProperty("latePayments").GetInt32());
        Assert.Equal(0, json.GetProperty("overduePayments").GetInt32());
    }

    [Fact]
    public async Task FinancialDisciplineScore_AfterPostponingMediumPayment_Returns66()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var plannedDate =
            DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var createRequest = new
        {
            categoryId = 1,
            amount = 500,
            plannedDate,
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

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var futurePaymentId =
            createJson.GetProperty("id").GetInt32();

        var postponeRequest = new
        {
            newPlannedDate = plannedDate.AddDays(5)
        };

        var postponeResponse = await client.PatchAsJsonAsync(
            $"/api/future-payments/{futurePaymentId}/postpone",
            postponeRequest
        );

        Assert.Equal(
            HttpStatusCode.OK,
            postponeResponse.StatusCode
        );

        var scoreResponse = await client.GetAsync(
            "/api/reports/financial-discipline-score"
        );

        var json =
            await scoreResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(66, json.GetProperty("score").GetInt32());
        Assert.Equal(1, json.GetProperty("postponements").GetInt32());
    }

    [Fact]
    public async Task FinancialDisciplineScore_WhenScoreWouldBeNegative_Returns0()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 5; i++)
        {
            var request = new
            {
                categoryId = 1,
                amount = 500,
                plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                importanceLevel = "high",
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

        var scoreResponse = await client.GetAsync(
            "/api/reports/financial-discipline-score"
        );

        var json =
            await scoreResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, json.GetProperty("score").GetInt32());
        Assert.Equal(5, json.GetProperty("overduePayments").GetInt32());
    }

    [Fact]
    public async Task FinancialDisciplineScore_WhenScoreWouldExceed100_Returns100()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 4; i++)
        {
            var request = new
            {
                categoryId = 1,
                amount = 500,
                plannedDate = DateOnly.FromDateTime(DateTime.Today),
                importanceLevel = "high",
                paymentMethodId = 1
            };

            var createResponse = await client.PostAsJsonAsync(
                "/api/future-payments",
                request
            );

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode
            );

            var createJson =
                await createResponse.Content.ReadFromJsonAsync<JsonElement>();

            var futurePaymentId =
                createJson.GetProperty("id").GetInt32();

            var payResponse = await client.PatchAsync(
                $"/api/future-payments/{futurePaymentId}/pay",
                null
            );

            Assert.Equal(
                HttpStatusCode.OK,
                payResponse.StatusCode
            );
        }

        var scoreResponse = await client.GetAsync(
            "/api/reports/financial-discipline-score"
        );

        var json =
            await scoreResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(100, json.GetProperty("score").GetInt32());
        Assert.Equal(4, json.GetProperty("onTimePayments").GetInt32());
    }

    [Fact]
    public async Task PayFuturePayment_BelongingToAnotherUser_ReturnsNotFound()
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
            plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            importanceLevel = "medium",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var futurePaymentId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.PatchAsync(
            $"/api/future-payments/{futurePaymentId}/pay",
            null
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task PostponeFuturePayment_BelongingToAnotherUser_ReturnsNotFound()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // User A
        var userAToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);

        var plannedDate =
            DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var createRequest = new
        {
            categoryId = 1,
            amount = 500,
            plannedDate,
            importanceLevel = "medium",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var futurePaymentId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var postponeRequest = new
        {
            newPlannedDate = plannedDate.AddDays(5)
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/future-payments/{futurePaymentId}/postpone",
            postponeRequest
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task DeleteFuturePayment_BelongingToAnotherUser_ReturnsNotFound()
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
            plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            importanceLevel = "medium",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var futurePaymentId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.DeleteAsync(
            $"/api/future-payments/{futurePaymentId}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task UpdateFuturePayment_BelongingToAnotherUser_ReturnsNotFound()
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
            plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            importanceLevel = "medium",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/future-payments",
            createRequest
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var futurePaymentId =
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
            importanceLevel = "high",
            paymentMethodId = 1
        };

        var response = await client.PutAsJsonAsync(
            $"/api/future-payments/{futurePaymentId}",
            updateRequest
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

}