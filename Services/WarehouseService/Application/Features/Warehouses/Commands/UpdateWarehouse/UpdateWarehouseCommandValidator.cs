using FluentValidation;

namespace Application.Features.Warehouses.Commands.UpdateWarehouse
{
    public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
    {
        public UpdateWarehouseCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Mã kho không được để trống");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên kho không được để trống")
                .MinimumLength(3).WithMessage("Tên kho phải có ít nhất 3 ký tự");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Địa chỉ không được để trống");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Sức chứa phải lớn hơn 0");
        }
    }
}
