using SharedLibrary.Seedwork;

namespace Domain.Entities
{
    public class Warehouse : BaseEntity<string>
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Capacity { get; set; }
        public string Status { get; set; } = "Active";
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
