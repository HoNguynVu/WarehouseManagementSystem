using MediatR;
using SharedLibrary.Responses;

using System.ComponentModel.DataAnnotations;

namespace Application.Features.Inventories.Commands.ConfirmStockOut
{
    public class ConfirmStockOutCommand : IRequest<ApiResponse<bool>>
    {
        public string WarehouseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã sản phẩm không được để trống.")]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Mã đơn hàng không được để trống.")]
        public string OrderId { get; set; } = string.Empty;
    }
}
