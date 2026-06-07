using Application.DTOs.Requests;
using Application.DTOs.Responses;
using SharedLibrary.Responses;

namespace Application.Services.Interfaces
{
    public interface ICustomerAddressService
    {
        Task<ApiResponse<IEnumerable<CustomerAddressResponse>>> GetByAccountIdAsync(string accountId);
        Task<ApiResponse<CustomerAddressResponse>> GetByIdAsync(string id, string accountId);
        Task<ApiResponse<CustomerAddressResponse>> CreateAsync(string accountId, CreateCustomerAddressRequest request);
        Task<ApiResponse<CustomerAddressResponse>> UpdateAsync(string id, string accountId, UpdateCustomerAddressRequest request);
        Task<ApiResponse<bool>> DeleteAsync(string id, string accountId);
        Task<ApiResponse<CustomerAddressResponse>> SetDefaultAsync(string id, string accountId);
    }
}
