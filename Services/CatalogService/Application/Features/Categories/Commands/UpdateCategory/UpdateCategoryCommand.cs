using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommand : IRequest<ApiResponse<CategoryDTO>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateCategoryDTO CategoryDto { get; set; } = new();
    }
}
