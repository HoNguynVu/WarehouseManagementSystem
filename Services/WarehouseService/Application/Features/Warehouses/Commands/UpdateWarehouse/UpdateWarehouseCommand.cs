using MediatR;
using SharedLibrary.Responses;
using Application.DTOs;

using System.ComponentModel.DataAnnotations;

namespace Application.Features.Warehouses.Commands.UpdateWarehouse
{
    public class UpdateWarehouseCommand : IRequest<ApiResponse<WarehouseDTO>>
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kho không được để trống")]
        [MinLength(3, ErrorMessage = "Tên kho phải có ít nhất 3 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = string.Empty;
        
        [Range(1, double.MaxValue, ErrorMessage = "Sức chứa phải lớn hơn 0")]
        public double Capacity { get; set; }
    }
}
