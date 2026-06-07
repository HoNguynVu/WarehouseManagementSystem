using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace CatalogService.Tests;

internal static class CacheMockExtensions
{
    public static void SetupJson<T>(this Mock<IDistributedCache> cache, string key, T value)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        cache.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(bytes);
    }

    public static void SetupMiss(this Mock<IDistributedCache> cache, string key)
    {
        cache.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
    }
}
