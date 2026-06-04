using System;
using System.Threading;

namespace SharedLibrary.Observability
{
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

        public static string? CorrelationId
        {
            get => CurrentCorrelationId.Value;
            set => CurrentCorrelationId.Value = value;
        }

        public static string GetOrCreate()
        {
            if (!string.IsNullOrWhiteSpace(CorrelationId))
            {
                return CorrelationId;
            }

            CorrelationId = Guid.NewGuid().ToString("N");
            return CorrelationId;
        }
    }
}
