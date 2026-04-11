using SharedLibrary.Seedwork;

namespace Domain.Entities
{
    public class Inventory : BaseEntity<string>
    {
        public string ProductId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public virtual Warehouse Warehouse { get; set; }
    }
}
