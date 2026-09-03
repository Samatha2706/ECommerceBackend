using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.DTOs.Auth;
using ECommerce.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.Integration;

public class AuthIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var email = $"user{Guid.NewGuid():N}@example.com";

        var request = new RegisterDto
        {
            FullName = "Integration Test User",
            Email = email,
            Password = "Test@12345"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"duplicate{Guid.NewGuid():N}@example.com";

        var request = new RegisterDto
        {
            FullName = "Test User",
            Email = email,
            Password = "Test@12345"
        };

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var email = $"login{Guid.NewGuid():N}@example.com";

        var registerRequest = new RegisterDto
        {
            FullName = "Login Test User",
            Email = email,
            Password = "Test@12345"
        };

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginRequest = new LoginDto
        {
            Email = email,
            Password = "Test@12345"
        };

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var result =
            await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var email = $"invalid{Guid.NewGuid():N}@example.com";

        var registerRequest = new RegisterDto
        {
            FullName = "Invalid Login User",
            Email = email,
            Password = "Test@12345"
        };

        await _client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        var loginRequest = new LoginDto
        {
            Email = email,
            Password = "WrongPassword@123"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}