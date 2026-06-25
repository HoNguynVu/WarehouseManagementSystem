using Application.DTOs;
using Application.Features.Categories.Commands.CreateCategory;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Responses;
using Xunit;

namespace CatalogService.Tests
{
    public class CreateCategoryCommandHandlerTests
    {
        private readonly Mock<ICategoryRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<ILogger<CreateCategoryCommandHandler>> _mockLogger;

        public CreateCategoryCommandHandlerTests()
        {
            _mockRepo = new Mock<ICategoryRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<CreateCategoryCommandHandler>>();
        }

        [Fact]
        public async Task Handle_CreatesCategory_Successfully()
        {
            var createDto = new CreateCategoryDTO { Name = "TestCategory" };
            var category = new Category { Name = "TestCategory" };
            var dto = new CategoryDTO { Id = "CAT-123", Name = "TestCategory" };

            _mockMapper.Setup(m => m.Map<Category>(createDto)).Returns(category);
            _mockMapper.Setup(m => m.Map<CategoryDTO>(It.IsAny<Category>())).Returns(dto);
            _mockRepo.Setup(r => r.CreateCategoryAsync(It.IsAny<Category>())).ReturnsAsync(category);

            var handler = new CreateCategoryCommandHandler(_mockRepo.Object, _mockMapper.Object, _mockCache.Object, _mockLogger.Object);
            
            var result = await handler.Handle(new CreateCategoryCommand { CategoryDto = createDto }, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("TestCategory", result.Data!.Name);
            _mockRepo.Verify(r => r.CreateCategoryAsync(It.IsAny<Category>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync("all_categories", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
