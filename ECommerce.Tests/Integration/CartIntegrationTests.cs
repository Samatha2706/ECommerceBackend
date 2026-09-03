using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.API;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Carts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.Integration;

public class CartIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();

        var email = $"cart{Guid.NewGuid():N}@example.com";

        var registerRequest = new RegisterDto
        {
            FullName = "Cart Integration User",
            Email = email,
            Password = "Test@12345"
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var authResponse =
            await registerResponse.Content
                .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(authResponse);
        Assert.False(
            string.IsNullOrWhiteSpace(authResponse.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authResponse.Token);

        return client;
    }

    [Fact]
    public async Task GetCart_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Cart");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCart_WithAuthentication_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "/api/Cart");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cart =
            await response.Content
                .ReadFromJsonAsync<CartDto>();

        Assert.NotNull(cart);
        Assert.NotNull(cart.Items);
    }

    [Fact]
    public async Task AddItem_WithInvalidProduct_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var request = new AddCartItemDto
        {
            ProductId = 999999,
            Quantity = 1
        };

        var response = await client.PostAsJsonAsync(
            "/api/Cart/items",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateItem_WithNonExistingCartItem_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var request = new UpdateCartItemDto
        {
            Quantity = 2
        };

        var response = await client.PutAsJsonAsync(
            "/api/Cart/items/999999",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_WithNonExistingCartItem_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync(
            "/api/Cart/items/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}