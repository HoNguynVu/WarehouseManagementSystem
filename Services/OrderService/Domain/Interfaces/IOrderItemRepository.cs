using Domain.Entities;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IOrderItemRepository : IGenericInterface<OrderItem, string>
    {
        Task<IEnumerable<OrderItem>> GetByOrderId(string orderId);
    }
}
