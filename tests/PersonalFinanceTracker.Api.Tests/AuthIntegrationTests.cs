using System.Net;
using System.Net.Http.Json;

namespace PersonalFinanceTracker.Api.Tests;

public class AuthIntegrationTests
{
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()


    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var request = new
        {
            name = "Test",
            surname = "User",
            phone = "5551112233",
            email = $"test-{Guid.NewGuid()}@example.com",
            password = "123456"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );

    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var email = $"login-{Guid.NewGuid()}@example.com";
        var password = "123456";

        var registerRequest = new
        {
            name = "Login",
            surname = "Test",
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
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var email = $"wrong-password-{Guid.NewGuid()}@example.com";

        var registerRequest = new
        {
            name = "Wrong",
            surname = "Password",
            phone = "5551112233",
            email,
            password = "123456"
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
            password = "wrongpassword"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode
        );
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var client = factory.CreateClient();

        var email = $"duplicate-{Guid.NewGuid()}@example.com";

        var request = new
        {
            name = "Duplicate",
            surname = "User",
            phone = "5551112233",
            email,
            password = "123456"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request
        );

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode
        );

        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            request
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode
        );
    }

}