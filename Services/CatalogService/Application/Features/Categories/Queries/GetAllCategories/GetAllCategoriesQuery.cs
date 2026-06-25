using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQuery : IRequest<ApiResponse<IEnumerable<CategoryDTO>>>
    {
    }
}
