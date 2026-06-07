using Application.DTOs;
using Application.Mappings;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CatalogService.Tests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<CatalogProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task GetCategoryByIdAsync_WhenCacheHit_ReturnsCachedDto()
    {
        _cache.SetupJson("category_CAT001", new CategoryDTO { Id = "CAT001", Name = "Cached" });
        var service = CreateService();

        var result = await service.GetCategoryByIdAsync("CAT001");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Cached");
        _categories.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenMissing_ReturnsNotFound()
    {
        _cache.SetupMiss("category_CAT404");
        _categories.Setup(x => x.GetByIdAsync("CAT404")).ReturnsAsync((Category?)null);
        var service = CreateService();

        var result = await service.GetCategoryByIdAsync("CAT404");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateCategoryAsync_WhenValid_CreatesAndInvalidatesListCache()
    {
        _categories.Setup(x => x.CreateCategoryAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category category) => category);
        var service = CreateService();

        var result = await service.CreateCategoryAsync(new CreateCategoryDTO { Name = "Tools", Description = "Warehouse tools" });

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Name.Should().Be("Tools");
        _cache.Verify(x => x.RemoveAsync("all_categories", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenNameChanges_UpdatesProductCategoryNames()
    {
        var category = new Category { Id = "CAT001", Name = "Old", Description = "Old desc" };
        _categories.Setup(x => x.GetByIdAsync("CAT001")).ReturnsAsync(category);
        _categories.Setup(x => x.UpdateCategoryAsync(category)).ReturnsAsync(true);
        var service = CreateService();

        var result = await service.UpdateCategoryAsync("CAT001", new UpdateCategoryDTO { Name = "New" });

        result.IsSuccess.Should().BeTrue();
        _products.Verify(x => x.UpdateCategoryNameForAllProductsAsync("CAT001", "New"), Times.Once);
        _cache.Verify(x => x.RemoveAsync("all_products", It.IsAny<CancellationToken>()), Times.Once);
    }

    private CategoryService CreateService()
    {
        return new CategoryService(
            _categories.Object,
            _products.Object,
            _mapper,
            _cache.Object,
            Mock.Of<ILogger<CategoryService>>());
    }
}
