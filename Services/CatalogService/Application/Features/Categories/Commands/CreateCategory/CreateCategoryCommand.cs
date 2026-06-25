using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommand : IRequest<ApiResponse<CategoryDTO>>
    {
        public CreateCategoryDTO CategoryDto { get; set; } = new();
    }
}
