using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQuery : IRequest<ApiResponse<CategoryDTO>>
    {
        public string Id { get; set; } = string.Empty;
    }
}
