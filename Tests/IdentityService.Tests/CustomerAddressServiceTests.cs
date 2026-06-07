using Application.DTOs.Requests;
using Application.Services.Implementions;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Infracstructure.UnitOfWorks;
using Moq;

namespace IdentityService.Tests;

public class CustomerAddressServiceTests
{
    private readonly Mock<IAuthUow> _uow = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ICustomerAddressRepository> _addresses = new();

    public CustomerAddressServiceTests()
    {
        _uow.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _uow.SetupGet(x => x.CustomerAddresses).Returns(_addresses.Object);
    }

    [Fact]
    public async Task GetByAccountIdAsync_WhenAccountIdMissing_ReturnsUnauthorized()
    {
        var service = new CustomerAddressService(_uow.Object);

        var result = await service.GetByAccountIdAsync("");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateAsync_WhenFirstAddress_CreatesDefaultAddress()
    {
        CustomerAddress? createdAddress = null;
        _accounts.Setup(x => x.GetByIdAsync("ACC001")).ReturnsAsync(new Accounts { Id = "ACC001" });
        _addresses.Setup(x => x.AnyForAccountAsync("ACC001")).ReturnsAsync(false);
        _addresses.Setup(x => x.GetByAccountIdAsync("ACC001")).ReturnsAsync(Array.Empty<CustomerAddress>());
        _addresses.Setup(x => x.Create(It.IsAny<CustomerAddress>())).Callback<CustomerAddress>(x => createdAddress = x);
        var service = new CustomerAddressService(_uow.Object);

        var result = await service.CreateAsync("ACC001", ValidCreateRequest(isDefault: false));

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        createdAddress.Should().NotBeNull();
        createdAddress!.IsDefault.Should().BeTrue();
        _uow.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task SetDefaultAsync_WhenAddressExists_ClearsOldDefault()
    {
        var oldDefault = Address("ADDR001", isDefault: true);
        var newDefault = Address("ADDR002", isDefault: false);
        _addresses.Setup(x => x.GetByIdForAccountAsync("ADDR002", "ACC001")).ReturnsAsync(newDefault);
        _addresses.Setup(x => x.GetByAccountIdAsync("ACC001")).ReturnsAsync(new[] { oldDefault, newDefault });
        var service = new CustomerAddressService(_uow.Object);

        var result = await service.SetDefaultAsync("ADDR002", "ACC001");

        result.IsSuccess.Should().BeTrue();
        oldDefault.IsDefault.Should().BeFalse();
        newDefault.IsDefault.Should().BeTrue();
        _addresses.Verify(x => x.Update(oldDefault), Times.Once);
        _addresses.Verify(x => x.Update(newDefault), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeletingDefault_PromotesNewestRemainingAddress()
    {
        var defaultAddress = Address("ADDR001", isDefault: true, createdAt: DateTimeOffset.UtcNow.AddDays(-3));
        var olderAddress = Address("ADDR002", isDefault: false, createdAt: DateTimeOffset.UtcNow.AddDays(-2));
        var newestAddress = Address("ADDR003", isDefault: false, createdAt: DateTimeOffset.UtcNow.AddDays(-1));
        _addresses.Setup(x => x.GetByIdForAccountAsync("ADDR001", "ACC001")).ReturnsAsync(defaultAddress);
        _addresses.Setup(x => x.GetByAccountIdAsync("ACC001")).ReturnsAsync(new[] { defaultAddress, olderAddress, newestAddress });
        var service = new CustomerAddressService(_uow.Object);

        var result = await service.DeleteAsync("ADDR001", "ACC001");

        result.IsSuccess.Should().BeTrue();
        newestAddress.IsDefault.Should().BeTrue();
        olderAddress.IsDefault.Should().BeFalse();
        _addresses.Verify(x => x.Delete(defaultAddress), Times.Once);
        _addresses.Verify(x => x.Update(newestAddress), Times.Once);
    }

    private static CreateCustomerAddressRequest ValidCreateRequest(bool isDefault)
    {
        return new CreateCustomerAddressRequest
        {
            ReceiverName = "Receiver",
            ReceiverPhone = "0900000000",
            Province = "Province",
            District = "District",
            Ward = "Ward",
            StreetAddress = "123 Street",
            IsDefault = isDefault
        };
    }

    private static CustomerAddress Address(string id, bool isDefault, DateTimeOffset? createdAt = null)
    {
        return new CustomerAddress
        {
            Id = id,
            AccountId = "ACC001",
            ReceiverName = "Receiver",
            ReceiverPhone = "0900000000",
            Province = "Province",
            District = "District",
            Ward = "Ward",
            StreetAddress = "123 Street",
            IsDefault = isDefault,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };
    }
}
