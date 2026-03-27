using SharedLibrary.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Seedwork
{
    // Thêm TId để linh hoạt kiểu dữ liệu của Khóa chính (int, string, Guid,...)
    public interface IGenericInterface<T, TId> where T : class
    {
        Task<T> CreateAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(T entity);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(TId id);
    }
}
