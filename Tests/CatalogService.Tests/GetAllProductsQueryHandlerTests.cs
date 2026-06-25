using Application.DTOs;
using Application.Features.Products.Queries.GetAllProducts;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Responses;
using System.Text.Json;
using Xunit;

namespace CatalogService.Tests
{
    public class GetAllProductsQueryHandlerTests
    {
        private readonly Mock<IProductRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<ILogger<GetAllProductsQueryHandler>> _mockLogger;

        public GetAllProductsQueryHandlerTests()
        {
            _mockRepo = new Mock<IProductRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<GetAllProductsQueryHandler>>();
        }

        [Fact]
        public async Task Handle_ReturnsProducts_FromCache_WhenCacheExists()
        {
            var cachedProducts = new List<ProductDTO> { new ProductDTO { Id = "1", Name = "TestProduct" } };
            var cachedData = JsonSerializer.Serialize(cachedProducts);
            
            _mockCache.Setup(c => c.GetAsync("all_products", It.IsAny<CancellationToken>()))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(cachedData));

            var handler = new GetAllProductsQueryHandler(_mockRepo.Object, _mockMapper.Object, _mockCache.Object, _mockLogger.Object);
            
            var result = await handler.Handle(new GetAllProductsQuery(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            _mockRepo.Verify(r => r.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ReturnsProducts_FromDb_WhenCacheEmpty()
        {
            _mockCache.Setup(c => c.GetAsync("all_products", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            var products = new List<Product> { new Product { Id = "1", Name = "TestProduct" } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

            var dtos = new List<ProductDTO> { new ProductDTO { Id = "1", Name = "TestProduct" } };
            _mockMapper.Setup(m => m.Map<IEnumerable<ProductDTO>>(products)).Returns(dtos);

            var handler = new GetAllProductsQueryHandler(_mockRepo.Object, _mockMapper.Object, _mockCache.Object, _mockLogger.Object);
            
            var result = await handler.Handle(new GetAllProductsQuery(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }
    }
}
