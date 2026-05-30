using System;

namespace SharedLibrary.Seedwork
{
    /// <summary>
    /// Base interface for Domain Events in the system.
    /// </summary>
    public interface IDomainEvent
    {
        DateTimeOffset OccurredOn { get; }
    }
}
