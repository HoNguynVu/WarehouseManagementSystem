using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedLibrary.Seedwork;

namespace Domain.Entities
{
    public class Product : BaseEntity <string>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [BsonElement("Price")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;    
        public string ImageUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Specifications { get; set; } = new Dictionary<string, string>();
    }
}
