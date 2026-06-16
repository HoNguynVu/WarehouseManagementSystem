using Domain.Entities;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IOrderRepository : IGenericInterface<Order, string>
    {
        Task<IEnumerable<Order>> GetByAccountIdAsync(string accountId);
    }
}
