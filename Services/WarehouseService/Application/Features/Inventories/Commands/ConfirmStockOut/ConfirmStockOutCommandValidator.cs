using FluentValidation;

namespace Application.Features.Inventories.Commands.ConfirmStockOut
{
    public class ConfirmStockOutCommandValidator : AbstractValidator<ConfirmStockOutCommand>
    {
        public ConfirmStockOutCommandValidator()
        {
            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("Mã kho hàng không được để trống");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Mã sản phẩm không được để trống");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0");

            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Mã đơn hàng không được để trống");
        }
    }
}
