using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Warehouses.Commands.DeleteWarehouse
{
    public class DeleteWarehouseCommand : IRequest<ApiResponse<bool>>
    {
        public string Id { get; set; } = string.Empty;

        public DeleteWarehouseCommand(string id)
        {
            Id = id;
        }

        public DeleteWarehouseCommand() { }
    }
}
