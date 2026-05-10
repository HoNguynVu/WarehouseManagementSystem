using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class OrderItemRepository : GenericRepositories<OrderItem, string>, IOrderItemRepository
    {
        public OrderItemRepository(OrderDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<OrderItem>> GetByOrderId(string orderId)
        {
            return await _dbSet.Where(oi => oi.OrderId == orderId).ToListAsync();
        }
    }
}
