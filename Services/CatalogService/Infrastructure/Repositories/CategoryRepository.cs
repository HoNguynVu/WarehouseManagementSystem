using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CatalogContext _context;
        public CategoryRepository(CatalogContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories.Find(_ => true).ToListAsync();
        }
        public async Task<Category?> GetByIdAsync(string id)
        {
            return await _context.Categories.Find(c => c.Id == id).FirstOrDefaultAsync();
        }
        public async Task<Category> CreateCategoryAsync(Category category)
        {
            await _context.Categories.InsertOneAsync(category);
            return category;
        }
        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            var result = await _context.Categories.ReplaceOneAsync(c => c.Id == category.Id, category);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        public async Task<bool> DeleteCategoryAsync(string id)
        {
            var result = await _context.Categories.DeleteOneAsync(c => c.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
        public void Create(Category entity)
        {
            _context.Categories.InsertOne(entity);
        }
        public void Update(Category entity)
        {
            _context.Categories.ReplaceOne(c => c.Id == entity.Id, entity);
        }
        public void Delete(Category entity)
        {
            _context.Categories.DeleteOne(c => c.Id == entity.Id);
        }
    }
}
