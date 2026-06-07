using Application.Mappings;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace WarehouseService.Tests;

internal static class WarehouseTestHelpers
{
    public static IMapper Mapper { get; } = new MapperConfiguration(cfg => cfg.AddProfile<WarehouseProfile>(), NullLoggerFactory.Instance).CreateMapper();
}
