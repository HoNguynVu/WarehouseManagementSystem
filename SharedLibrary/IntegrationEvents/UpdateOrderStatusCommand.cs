namespace SharedLibrary.IntegrationEvents
{
    public class UpdateOrderStatusCommand
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
