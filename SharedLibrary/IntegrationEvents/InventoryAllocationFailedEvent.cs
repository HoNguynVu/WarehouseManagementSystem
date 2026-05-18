namespace SharedLibrary.IntegrationEvents
{
    public record InventoryAllocationFailedEvent
    {
        public string OrderId { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }
}
