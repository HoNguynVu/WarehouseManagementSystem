namespace Application.DTOs
{
    public class WarehouseDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Capacity { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
