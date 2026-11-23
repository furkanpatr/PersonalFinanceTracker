using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class RecurringPaymentIntegrationTests
{
    [Fact]
    public async Task ProcessDue_WithDueRecurringPayment_CreatesFuturePayment()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var recurringRequest = new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "Test Recurring Payment",
            amount = 500,
            frequency = "monthly",
            nextDueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            importanceLevel = "medium",
            description = "Integration test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/recurring-payments",
            recurringRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var processResponse = await client.PostAsync(
            "/api/recurring-payments/process-due",
            null
        );

        Assert.Equal(
            HttpStatusCode.OK,
            processResponse.StatusCode
        );

        var json =
            await processResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            1,
            json.GetProperty("createdCount").GetInt32()
        );
    }

    [Fact]
    public async Task ProcessDue_WhenOccurrenceAlreadyExists_ReturnsCreatedCountZero()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var dueDate =
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        var recurringRequest = new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "Duplicate Test",
            amount = 500,
            frequency = "monthly",
            nextDueDate = dueDate,
            importanceLevel = "medium",
            description = "Duplicate prevention test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/recurring-payments",
            recurringRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var firstProcessResponse = await client.PostAsync(
            "/api/recurring-payments/process-due",
            null
        );

        var firstJson =
            await firstProcessResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            1,
            firstJson.GetProperty("createdCount").GetInt32()
        );

        var secondProcessResponse = await client.PostAsync(
            "/api/recurring-payments/process-due",
            null
        );

        var secondJson =
            await secondProcessResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            0,
            secondJson.GetProperty("createdCount").GetInt32()
        );
    }

    [Fact]
    public async Task ProcessDue_WithInactiveRecurringPayment_ReturnsCreatedCountZero()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var recurringRequest = new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "Inactive Recurring Payment",
            amount = 500,
            frequency = "monthly",
            nextDueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            importanceLevel = "medium",
            description = "Inactive recurring test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/recurring-payments",
            recurringRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var recurringPaymentId =
            createJson.GetProperty("id").GetInt32();

        var statusRequest = new
        {
            isActive = false
        };

        var statusResponse = await client.PatchAsJsonAsync(
            $"/api/recurring-payments/{recurringPaymentId}/status",
            statusRequest
        );

        Assert.Equal(
            HttpStatusCode.OK,
            statusResponse.StatusCode
        );

        var processResponse = await client.PostAsync(
            "/api/recurring-payments/process-due",
            null
        );

        Assert.Equal(
            HttpStatusCode.OK,
            processResponse.StatusCode
        );

        var json =
            await processResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            0,
            json.GetProperty("createdCount").GetInt32()
        );
    }

    [Fact]
    public async Task DeleteRecurringPayment_BelongingToAnotherUser_ReturnsNotFound()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // User A
        var userAToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);

        var recurringRequest = new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "User Scoped Recurring",
            amount = 500,
            frequency = "monthly",
            nextDueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            importanceLevel = "medium",
            description = "User scoping test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/recurring-payments",
            recurringRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var recurringPaymentId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var response = await client.DeleteAsync(
            $"/api/recurring-payments/{recurringPaymentId}"
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task UpdateRecurringPaymentStatus_BelongingToAnotherUser_ReturnsNotFound()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        // User A
        var userAToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAToken);

        var recurringRequest = new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "Status Scope Test",
            amount = 500,
            frequency = "monthly",
            nextDueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            importanceLevel = "medium",
            description = "Status user-scoping test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/recurring-payments",
            recurringRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode
        );

        var createJson =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var recurringPaymentId =
            createJson.GetProperty("id").GetInt32();

        // User B
        var userBToken =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userBToken);

        var statusRequest = new
        {
            isActive = false
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/recurring-payments/{recurringPaymentId}/status",
            statusRequest
        );

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode
        );
    }

    [Fact]
    public async Task ProcessDue_MonthEnd_PreservesOriginalDay()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var token =
            await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
            "/api/recurring-payments",
            new
            {
                categoryId = 1,
                paymentMethodId = 1,
                name = "Month End Test",
                amount = 500,
                frequency = "monthly",
                nextDueDate = new DateOnly(2025, 1, 31),
                importanceLevel = "medium",
                description = "Month end date test"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = created.GetProperty("id").GetInt32();

        for (int i = 0; i < 2; i++)
        {
            var response = await client.PostAsync(
                "/api/recurring-payments/process-due", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var getResponse =
            await client.GetAsync("/api/recurring-payments");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var payments =
            await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        var payment = payments.EnumerateArray()
            .First(p => p.GetProperty("id").GetInt32() == id);

        Assert.Equal(
            "2025-03-31",
            payment.GetProperty("nextDueDate").GetString());
    }

    [Fact]
    public async Task ProcessDue_LeapYear_PreservesFebruary29()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var token = await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
        "/api/recurring-payments",
        new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "Leap Year Test",
            amount = 500,
            frequency = "yearly",
            nextDueDate = new DateOnly(2020, 2, 29),
            importanceLevel = "medium",
            description = "Leap year date test"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created =
        await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = created.GetProperty("id").GetInt32();

        for (int i = 0; i < 4; i++)
        {
            var response = await client.PostAsync(
            "/api/recurring-payments/process-due", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var getResponse =
        await client.GetAsync("/api/recurring-payments");

        var payments =
        await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        var payment = payments.EnumerateArray()
        .First(p => p.GetProperty("id").GetInt32() == id);

        Assert.Equal(
        "2024-02-29",
        payment.GetProperty("nextDueDate").GetString());
    }

    [Fact]
    public async Task ProcessDue_Day30_PreservesOriginalDay()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var token = await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
        "/api/recurring-payments",
        new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "Day 30 Test",
            amount = 500,
            frequency = "monthly",
            nextDueDate = new DateOnly(2025, 1, 30),
            importanceLevel = "medium",
            description = "Monthly day 30 test"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created =
        await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = created.GetProperty("id").GetInt32();

        for (int i = 0; i < 2; i++)
        {
            var response = await client.PostAsync(
            "/api/recurring-payments/process-due", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var getResponse =
        await client.GetAsync("/api/recurring-payments");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var payments =
        await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        var payment = payments.EnumerateArray()
        .First(p => p.GetProperty("id").GetInt32() == id);

        Assert.Equal(
        "2025-03-30",
        payment.GetProperty("nextDueDate").GetString());
    }

    [Fact]
    public async Task ProcessDue_Day29_PreservesOriginalDay()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var token = await TestAuthHelper.RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
        "/api/recurring-payments",
        new
        {
            categoryId = 1,
            paymentMethodId = 1,
            name = "Day 29 Test",
            amount = 500,
            frequency = "monthly",
            nextDueDate = new DateOnly(2025, 1, 29),
            importanceLevel = "medium",
            description = "Monthly day 29 test"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created =
        await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var id = created.GetProperty("id").GetInt32();

        for (int i = 0; i < 2; i++)
        {
            var response = await client.PostAsync(
            "/api/recurring-payments/process-due", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var getResponse =
        await client.GetAsync("/api/recurring-payments");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var payments =
        await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        var payment = payments.EnumerateArray()
        .First(p => p.GetProperty("id").GetInt32() == id);

        Assert.Equal(
        "2025-03-29",
        payment.GetProperty("nextDueDate").GetString());
    }

}
