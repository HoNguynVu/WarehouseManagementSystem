using Domain.Entities;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IPaymentRepository : IGenericInterface<Payment, string>
    {
        Task<Payment?> GetByOrderIdAsync(string orderId);
        Task<Payment?> GetByTransactionIdAsync(string transactionId);
    }
}
