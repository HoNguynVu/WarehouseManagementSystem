using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Infracstructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infracstructure.Repositories
{
    public class GenericRepositories<T, TId> : IGenericInterface<T, TId> where T : class
    {
        protected readonly IdentityDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepositories(IdentityDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual void Create(T entity)
        {
            _dbSet.Add(entity);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(TId id)
        {
            return await _dbSet.FindAsync(id);
        }
    } 
}
