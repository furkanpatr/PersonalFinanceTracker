using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class ReportIntegrationTests
{
    [Fact]
    public async Task RangeReport_WithStartDateAfterEndDate_ReturnsBadRequest()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var startDate =
            DateOnly.FromDateTime(DateTime.Today);

        var endDate =
            startDate.AddDays(-5);

        var response = await client.GetAsync(
            $"/api/reports/range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task RangeReport_WithExpense_ReturnsCorrectTotals()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var today =
            DateOnly.FromDateTime(DateTime.Today);

        var expenseRequest = new
        {
            categoryId = 1,
            amount = 300,
            date = today,
            place = "Test Market",
            description = "Range report test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            expenseRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var response = await client.GetAsync(
            $"/api/reports/range?startDate={today:yyyy-MM-dd}&endDate={today:yyyy-MM-dd}"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            300m,
            json.GetProperty("totalExpense").GetDecimal()
        );

        Assert.Equal(
            1,
            json.GetProperty("expenseCount").GetInt64()
        );
    }

    [Fact]
    public async Task SummaryReport_WithHighPendingFuturePayment_ReturnsCorrectTotals()
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
            amount = 1200,
            plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
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

        var response = await client.GetAsync(
            "/api/reports/summary"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            1200m,
            json.GetProperty("upcomingPaymentsTotal").GetDecimal()
        );

        Assert.Equal(
            1200m,
            json.GetProperty("highImportanceUpcomingTotal").GetDecimal()
        );
    }

    [Fact]
    public async Task CategoryReport_WithExpense_ReturnsCorrectTotals()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var expenseRequest = new
        {
            categoryId = 1,
            amount = 400,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Category report test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            expenseRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var response = await client.GetAsync(
            "/api/reports/categories"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            1,
            json.GetArrayLength()
        );

        var category =
            json[0];

        Assert.Equal(
            400m,
            category.GetProperty("totalExpense").GetDecimal()
        );

        Assert.Equal(
            1,
            category.GetProperty("expenseCount").GetInt64()
        );
    }

    [Fact]
    public async Task MonthlyReport_WithCurrentMonthExpense_ReturnsCorrectTotal()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var expenseRequest = new
        {
            categoryId = 1,
            amount = 600,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Monthly report test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            expenseRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var response = await client.GetAsync(
            "/api/reports/monthly"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            600m,
            json.GetProperty("currentMonthTotal").GetDecimal()
        );

        Assert.Equal(
            0m,
            json.GetProperty("previousMonthTotal").GetDecimal()
        );

        Assert.Equal(
            600m,
            json.GetProperty("difference").GetDecimal()
        );
    }

    [Fact]
    public async Task WeeklyReport_WithCurrentWeekExpense_ReturnsCorrectTotal()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var expenseRequest = new
        {
            categoryId = 1,
            amount = 350,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Weekly report test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            expenseRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var response = await client.GetAsync(
            "/api/reports/weekly"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            350m,
            json.GetProperty("currentWeekTotal").GetDecimal()
        );

        Assert.Equal(
            0m,
            json.GetProperty("previousWeekTotal").GetDecimal()
        );

        Assert.Equal(
            350m,
            json.GetProperty("difference").GetDecimal()
        );
    }

    [Fact]
    public async Task FuturePaymentStatusReport_WithPendingPayment_ReturnsCorrectTotals()
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
            amount = 800,
            plannedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            importanceLevel = "medium",
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

        var response = await client.GetAsync(
            "/api/reports/future-payments/status"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            1,
            json.GetArrayLength()
        );

        var status = json[0];

        Assert.Equal(
            "pending",
            status.GetProperty("status").GetString()
        );

        Assert.Equal(
            1,
            status.GetProperty("paymentCount").GetInt64()
        );

        Assert.Equal(
            800m,
            status.GetProperty("totalAmount").GetDecimal()
        );
    }

    [Fact]
    public async Task SummaryReport_DoesNotIncludeAnotherUsersExpenses()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // User A
        var userAToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);

        var expenseRequest = new
        {
            categoryId = 1,
            amount = 900,
            date = DateOnly.FromDateTime(DateTime.Today),
            place = "Test Market",
            description = "Report user-scoping test",
            paymentMethodId = 1
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/expenses",
            expenseRequest
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
            "/api/reports/summary"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            0m,
            json.GetProperty("totalExpense").GetDecimal()
        );

        Assert.Equal(
            0,
            json.GetProperty("expenseCount").GetInt64()
        );
    }

    [Fact]
    public async Task SummaryReport_WithMultipleExpenses_ReturnsCorrectStatistics()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var firstExpense = new
        {
            categoryId = 1,
            amount = 300,
            date = today,
            place = "Test Market",
            description = "First expense",
            paymentMethodId = 1
        };

        var secondExpense = new
        {
            categoryId = 1,
            amount = 700,
            date = today,
            place = "Test Market",
            description = "Second expense",
            paymentMethodId = 1
        };

        await client.PostAsJsonAsync(
            "/api/expenses",
            firstExpense
        );

        await client.PostAsJsonAsync(
            "/api/expenses",
            secondExpense
        );

        var response = await client.GetAsync(
            "/api/reports/summary"
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            1000m,
            json.GetProperty("totalExpense").GetDecimal()
        );

        Assert.Equal(
            2,
            json.GetProperty("expenseCount").GetInt64()
        );

        Assert.Equal(
            500m,
            json.GetProperty("averageExpense").GetDecimal()
        );

        Assert.Equal(
            700m,
            json.GetProperty("highestExpense").GetDecimal()
        );

        Assert.Equal(
            300m,
            json.GetProperty("lowestExpense").GetDecimal()
        );
    }
}