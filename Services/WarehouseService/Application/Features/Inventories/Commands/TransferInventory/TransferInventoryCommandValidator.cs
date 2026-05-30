using FluentValidation;

namespace Application.Features.Inventories.Commands.TransferInventory
{
    public class TransferInventoryCommandValidator : AbstractValidator<TransferInventoryCommand>
    {
        public TransferInventoryCommandValidator()
        {
            RuleFor(x => x.FromWarehouseId)
                .NotEmpty().WithMessage("Kho xuất (fromWarehouseId) không được để trống");

            RuleFor(x => x.ToWarehouseId)
                .NotEmpty().WithMessage("Kho nhận (toWarehouseId) không được để trống")
                .NotEqual(x => x.FromWarehouseId).WithMessage("Kho nhận phải khác kho xuất");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Mã sản phẩm không được để trống");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng chuyển phải lớn hơn 0");
        }
    }
}
