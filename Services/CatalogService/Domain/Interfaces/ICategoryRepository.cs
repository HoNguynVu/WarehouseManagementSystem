using Domain.Entities;
using SharedLibrary.Seedwork;

namespace Domain.Interfaces
{
    public interface ICategoryRepository : IGenericInterface<Category, string>
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(string id);
        Task<Category> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(string id);
        void Update(Category category);
        void Delete(Category category);
    }
}
