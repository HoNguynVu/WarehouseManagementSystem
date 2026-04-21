using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly CatalogContext _context;
        public ProductRepository(CatalogContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.Find(_ => true).ToListAsync();
        }
        public async Task<Product?> GetByIdAsync(string id)
        {
            return await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }
        public async Task<Product> CreateProductAsync(Product product)
        {
            await _context.Products.InsertOneAsync(product);
            return product;
        }
        public async Task<bool> UpdateProductAsync(Product product)
        {
            var result = await _context.Products.ReplaceOneAsync(p => p.Id == product.Id, product);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        public async Task<bool> DeleteProductAsync(string id)
        {
            var result = await _context.Products.DeleteOneAsync(p => p.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
        public void Create(Product entity)
        {
            _context.Products.InsertOne(entity);
        }
        public void Update(Product product)
        {
            _context.Products.ReplaceOne(p => p.Id == product.Id, product);
        }
        public void Delete(Product product)
        {
            _context.Products.DeleteOne(p => p.Id == product.Id);
        }
    }
}
