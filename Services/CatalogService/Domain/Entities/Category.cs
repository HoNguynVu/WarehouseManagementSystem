using SharedLibrary.Seedwork;

namespace Domain.Entities
{
    public class Category : BaseEntity<string>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
