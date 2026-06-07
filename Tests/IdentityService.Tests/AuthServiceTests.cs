using Application.DTOs.Requests;
using Application.Helpers;
using Application.Mappings;
using Application.Services.Implementions;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using FluentAssertions;
using Infracstructure.UnitOfWorks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace IdentityService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuthUow> _uow = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IOtpRepository> _otps = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<ICustomerAddressRepository> _addresses = new();

    public AuthServiceTests()
    {
        _uow.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _uow.SetupGet(x => x.Otps).Returns(_otps.Object);
        _uow.SetupGet(x => x.RefreshTokens).Returns(_refreshTokens.Object);
        _uow.SetupGet(x => x.CustomerAddresses).Returns(_addresses.Object);
    }

    [Fact]
    public async Task SignUpAsync_WhenFullNameMissing_ReturnsBadRequest()
    {
        var service = CreateService();

        var result = await service.SignUpAsync(new SignUpRequest
        {
            Username = "user1",
            Email = "user1@test.com",
            Password = "Password123"
        });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Full name is required.");
        _accounts.Verify(x => x.Create(It.IsAny<Accounts>()), Times.Never);
    }

    [Fact]
    public async Task SignUpAsync_WhenEmailAlreadyExists_ReturnsConflict()
    {
        _accounts.Setup(x => x.GetByEmailAsync("taken@test.com"))
            .ReturnsAsync(new Accounts { Id = "ACC001", Email = "taken@test.com", Status = AccountStatus.Active });
        var service = CreateService();

        var result = await service.SignUpAsync(new SignUpRequest
        {
            Username = "user1",
            FullName = "Test User",
            Email = "taken@test.com",
            Password = "Password123"
        });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task SignUpAsync_WhenValid_CreatesAccountAndOtp()
    {
        Accounts? createdAccount = null;
        Otps? createdOtp = null;
        _accounts.Setup(x => x.GetByEmailAsync("new@test.com")).ReturnsAsync((Accounts?)null);
        _accounts.Setup(x => x.Create(It.IsAny<Accounts>())).Callback<Accounts>(x => createdAccount = x);
        _otps.Setup(x => x.Create(It.IsAny<Otps>())).Callback<Otps>(x => createdOtp = x);
        _emailService.Setup(x => x.SendVerificationEmail("new@test.com", It.IsAny<string>())).Returns(Task.CompletedTask);
        var service = CreateService();

        var result = await service.SignUpAsync(new SignUpRequest
        {
            Username = "user1",
            FullName = "New User",
            Email = "new@test.com",
            Password = "Password123",
            Phone = "0900000000"
        });

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        createdAccount.Should().NotBeNull();
        createdAccount!.FullName.Should().Be("New User");
        createdAccount.Password.Should().NotBe("Password123");
        createdOtp.Should().NotBeNull();
        createdOtp!.Purpose.Should().Be(OtpPurposes.EmailVerification);
        _uow.Verify(x => x.CommitAsync(), Times.Once);
        _emailService.Verify(x => x.SendVerificationEmail("new@test.com", createdOtp.Code), Times.Once);
    }

    private AuthService CreateService()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AuthProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var jwt = new JwtGenerator(Options.Create(new JwtSettings
        {
            SecretKey = "1234567890123456789012345678901234567890123456789012345678901234",
            Issuer = "test",
            Audience = "test",
            ExpirationMinutes = 15
        }));

        return new AuthService(_emailService.Object, _uow.Object, jwt, mapper);
    }
}
