using Domain.Entities;
using SharedLibrary.Seedwork;

namespace Domain.Interfaces
{
    public interface IProductRepository : IGenericInterface<Product, string>
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(string id);
        Task<Product> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> UpdateCategoryNameForAllProductsAsync(string categoryId, string newCategoryName);
        Task<bool> DeleteProductAsync(string id);
        void Update(Product product);
        void Delete(Product product);
    }
}
