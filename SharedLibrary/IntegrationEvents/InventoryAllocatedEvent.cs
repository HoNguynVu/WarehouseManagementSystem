namespace SharedLibrary.IntegrationEvents
{
    public record InventoryAllocatedEvent
    {
        public string OrderId { get; init; } = string.Empty;
    }
}
