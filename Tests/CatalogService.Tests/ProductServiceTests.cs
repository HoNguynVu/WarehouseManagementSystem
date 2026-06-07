using Application.DTOs;
using Application.Mappings;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SharedLibrary.IntegrationEvents;

namespace CatalogService.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<CatalogProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task GetProductByIdAsync_WhenCacheHit_ReturnsCachedDto()
    {
        _cache.SetupJson("product_PRD001", new ProductDTO { Id = "PRD001", Name = "Cached product" });
        var service = CreateService();

        var result = await service.GetProductByIdAsync("PRD001");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Cached product");
        _products.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_WhenCategoryMissing_ReturnsBadRequest()
    {
        _categories.Setup(x => x.GetByIdAsync("CAT404")).ReturnsAsync((Category?)null);
        var service = CreateService();

        var result = await service.CreateProductAsync(new CreateProductDTO
        {
            Name = "Product",
            Description = "Desc",
            Price = 10,
            CategoryId = "CAT404"
        });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        _products.Verify(x => x.CreateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenNameChanges_PublishesUpdateProductEvent()
    {
        var product = new Product { Id = "PRD001", Name = "Old", CategoryId = "CAT001", CategoryName = "Cat", Price = 10 };
        _products.Setup(x => x.GetByIdAsync("PRD001")).ReturnsAsync(product);
        _products.Setup(x => x.UpdateProductAsync(product)).ReturnsAsync(true);
        var service = CreateService();

        var result = await service.UpdateProductAsync("PRD001", new UpdateProductDTO { Name = "New" });

        result.IsSuccess.Should().BeTrue();
        _publishEndpoint.Verify(x => x.Publish(
            It.Is<UpdateProductEvent>(e => e.ProductId == "PRD001" && e.ProductName == "New"),
            It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(x => x.RemoveAsync("product_PRD001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenMissing_ReturnsNotFound()
    {
        _products.Setup(x => x.GetByIdAsync("PRD404")).ReturnsAsync((Product?)null);
        var service = CreateService();

        var result = await service.DeleteProductAsync("PRD404");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    private ProductService CreateService()
    {
        return new ProductService(
            _products.Object,
            _categories.Object,
            _mapper,
            _cache.Object,
            Mock.Of<ILogger<ProductService>>(),
            _publishEndpoint.Object);
    }
}
