using Domain.Entities;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.Data
{
    public class CatalogContext
    {
        public IMongoCollection<Product> Products { get; }
        public IMongoCollection<Category> Categories { get; }
        public CatalogContext(IMongoClient mongoClient, IOptions<MongoDbSettings> settings)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            Products = database.GetCollection<Product>(settings.Value.ProductsCollectionName);
            Categories = database.GetCollection<Category>(settings.Value.CategoriesCollectionName);
        }
    }
}
