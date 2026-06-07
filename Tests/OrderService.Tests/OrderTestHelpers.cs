using System.Net;
using AutoMapper;
using Application.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

namespace OrderService.Tests;

internal static class OrderTestHelpers
{
    public static IMapper Mapper { get; } = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>(), NullLoggerFactory.Instance).CreateMapper();
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public StubHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
