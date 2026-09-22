using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonalFinanceTracker.Api.Tests;

public static class TestAuthHelper
{
    public static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        var email = $"auth-{Guid.NewGuid()}@example.com";
        var password = "123456";

        var registerRequest = new
        {
            name = "Test",
            surname = "User",
            phone = "5551112233",
            email,
            password
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            registerRequest
        );

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode
        );

        var loginRequest = new
        {
            email,
            password
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest
        );

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode
        );

        var json =
            await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        return json.GetProperty("token").GetString()!;
    }
}