using FluentValidation;

namespace Application.Features.Inventories.Commands.AddInventory
{
    public class AddInventoryCommandValidator : AbstractValidator<AddInventoryCommand>
    {
        public AddInventoryCommandValidator()
        {
            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("Mã kho hàng không được để trống");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Mã sản phẩm không được để trống");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0");
        }
    }
}
