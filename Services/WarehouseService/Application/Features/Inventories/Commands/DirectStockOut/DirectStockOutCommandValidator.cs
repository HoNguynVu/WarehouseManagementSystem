using FluentValidation;

namespace Application.Features.Inventories.Commands.DirectStockOut
{
    public class DirectStockOutCommandValidator : AbstractValidator<DirectStockOutCommand>
    {
        public DirectStockOutCommandValidator()
        {
            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("Mã kho hàng không được để trống");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Mã sản phẩm không được để trống");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng xuất kho phải lớn hơn 0");
        }
    }
}
