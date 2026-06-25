using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<ApiResponse<bool>>
    {
        public string Id { get; set; } = string.Empty;
    }
}
