using ECommerce.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Tests.Integration;

public class ProductIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllProducts_ReturnsOk()
    {
        var response = await _client.GetAsync(
            "/api/Products");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_WithNonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            "/api/Products/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Products",
            new
            {
                name = "Integration Test Product",
                description = "Test product",
                price = 100,
                categoryId = 1,
                initialQuantity = 10,
                reorderLevel = 2
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/Products/1",
            new
            {
                name = "Updated Product",
                description = "Updated",
                price = 150,
                categoryId = 1,
                isActive = true
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync(
            "/api/Products/1");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}