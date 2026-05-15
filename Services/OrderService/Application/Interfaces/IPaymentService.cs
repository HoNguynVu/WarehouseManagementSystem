using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPaymentService
    {
        Task<(int StatusCode, PaymentLinkDTO dto)> CreateZaloPayLinkForOrder(string orderId, decimal amount);
        Task<bool> ProcessCallback(ZaloPayCallbackDTO cbdata);
    }
}
