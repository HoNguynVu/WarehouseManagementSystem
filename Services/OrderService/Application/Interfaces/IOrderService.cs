using Application.DTOs;
using SharedLibrary.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderDTO>> CreateOrderAsync(CreateOrderDTO dto);
        Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(string id);
        Task<ApiResponse<IEnumerable<OrderDTO>>> GetAllOrdersAsync();
    }
}
