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
    public class OrderRepository : GenericRepositories<Order, string>, IOrderRepository
    {
        public OrderRepository(OrderDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Order>> GetByAccountIdAsync(string accountId)
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                .Where(o => o.AccountId == accountId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public override async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}
