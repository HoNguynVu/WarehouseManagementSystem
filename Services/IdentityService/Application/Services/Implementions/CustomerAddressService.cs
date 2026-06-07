using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Helpers;
using Application.Services.Interfaces;
using Domain.Entities;
using Infracstructure.UnitOfWorks;
using SharedLibrary.Responses;

namespace Application.Services.Implementions
{
    public class CustomerAddressService : ICustomerAddressService
    {
        private readonly IAuthUow _authUow;

        public CustomerAddressService(IAuthUow authUow)
        {
            _authUow = authUow;
        }

        public async Task<ApiResponse<IEnumerable<CustomerAddressResponse>>> GetByAccountIdAsync(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return ApiResponse<IEnumerable<CustomerAddressResponse>>.Failure("Unauthorized.", 401);

            var addresses = await _authUow.CustomerAddresses.GetByAccountIdAsync(accountId);
            return ApiResponse<IEnumerable<CustomerAddressResponse>>.Success(addresses.Select(ToResponse));
        }

        public async Task<ApiResponse<CustomerAddressResponse>> GetByIdAsync(string id, string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return ApiResponse<CustomerAddressResponse>.Failure("Unauthorized.", 401);

            var address = await _authUow.CustomerAddresses.GetByIdForAccountAsync(id, accountId);
            if (address == null)
                return ApiResponse<CustomerAddressResponse>.Failure("Address not found.", 404);

            return ApiResponse<CustomerAddressResponse>.Success(ToResponse(address));
        }

        public async Task<ApiResponse<CustomerAddressResponse>> CreateAsync(string accountId, CreateCustomerAddressRequest request)
        {
            var validationError = ValidateAccountAndAddress(accountId, request?.ReceiverName, request?.ReceiverPhone, request?.Province, request?.District, request?.Ward, request?.StreetAddress);
            if (validationError != null)
                return ApiResponse<CustomerAddressResponse>.Failure(validationError.Message, validationError.StatusCode);

            var account = await _authUow.Accounts.GetByIdAsync(accountId);
            if (account == null)
                return ApiResponse<CustomerAddressResponse>.Failure("Account not found.", 404);

            var now = DateTimeOffset.UtcNow;
            var hasAnyAddress = await _authUow.CustomerAddresses.AnyForAccountAsync(accountId);
            var address = new CustomerAddress
            {
                Id = IdGenerator.GenerateId(),
                AccountId = accountId,
                ReceiverName = request!.ReceiverName.Trim(),
                ReceiverPhone = request.ReceiverPhone.Trim(),
                Province = request.Province.Trim(),
                District = request.District.Trim(),
                Ward = request.Ward.Trim(),
                StreetAddress = request.StreetAddress.Trim(),
                IsDefault = !hasAnyAddress || request.IsDefault,
                CreatedAt = now
            };

            await _authUow.BeginTransactionAsync();
            try
            {
                if (address.IsDefault)
                    await ClearDefaultAddresses(accountId, now);

                _authUow.CustomerAddresses.Create(address);
                await _authUow.CommitAsync();
                return ApiResponse<CustomerAddressResponse>.Success(ToResponse(address), "Address created successfully.", 201);
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<CustomerAddressResponse>.Failure($"Database error: {GetActualError(ex)}", 500);
            }
        }

        public async Task<ApiResponse<CustomerAddressResponse>> UpdateAsync(string id, string accountId, UpdateCustomerAddressRequest request)
        {
            var validationError = ValidateAccountAndAddress(accountId, request?.ReceiverName, request?.ReceiverPhone, request?.Province, request?.District, request?.Ward, request?.StreetAddress);
            if (validationError != null)
                return ApiResponse<CustomerAddressResponse>.Failure(validationError.Message, validationError.StatusCode);

            var address = await _authUow.CustomerAddresses.GetByIdForAccountAsync(id, accountId);
            if (address == null)
                return ApiResponse<CustomerAddressResponse>.Failure("Address not found.", 404);

            address.ReceiverName = request!.ReceiverName.Trim();
            address.ReceiverPhone = request.ReceiverPhone.Trim();
            address.Province = request.Province.Trim();
            address.District = request.District.Trim();
            address.Ward = request.Ward.Trim();
            address.StreetAddress = request.StreetAddress.Trim();
            address.UpdatedAt = DateTimeOffset.UtcNow;

            await _authUow.BeginTransactionAsync();
            try
            {
                _authUow.CustomerAddresses.Update(address);
                await _authUow.CommitAsync();
                return ApiResponse<CustomerAddressResponse>.Success(ToResponse(address), "Address updated successfully.");
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<CustomerAddressResponse>.Failure($"Database error: {GetActualError(ex)}", 500);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return ApiResponse<bool>.Failure("Unauthorized.", 401);

            var address = await _authUow.CustomerAddresses.GetByIdForAccountAsync(id, accountId);
            if (address == null)
                return ApiResponse<bool>.Failure("Address not found.", 404);

            var remainingAddresses = (await _authUow.CustomerAddresses.GetByAccountIdAsync(accountId))
                .Where(a => a.Id != id)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            await _authUow.BeginTransactionAsync();
            try
            {
                _authUow.CustomerAddresses.Delete(address);

                if (address.IsDefault && remainingAddresses.Any())
                {
                    var nextDefault = remainingAddresses.First();
                    nextDefault.IsDefault = true;
                    nextDefault.UpdatedAt = DateTimeOffset.UtcNow;
                    _authUow.CustomerAddresses.Update(nextDefault);
                }

                await _authUow.CommitAsync();
                return ApiResponse<bool>.Success(true, "Address deleted successfully.");
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<bool>.Failure($"Database error: {GetActualError(ex)}", 500);
            }
        }

        public async Task<ApiResponse<CustomerAddressResponse>> SetDefaultAsync(string id, string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return ApiResponse<CustomerAddressResponse>.Failure("Unauthorized.", 401);

            var address = await _authUow.CustomerAddresses.GetByIdForAccountAsync(id, accountId);
            if (address == null)
                return ApiResponse<CustomerAddressResponse>.Failure("Address not found.", 404);

            var now = DateTimeOffset.UtcNow;
            await _authUow.BeginTransactionAsync();
            try
            {
                await ClearDefaultAddresses(accountId, now);
                address.IsDefault = true;
                address.UpdatedAt = now;
                _authUow.CustomerAddresses.Update(address);
                await _authUow.CommitAsync();
                return ApiResponse<CustomerAddressResponse>.Success(ToResponse(address), "Default address updated successfully.");
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<CustomerAddressResponse>.Failure($"Database error: {GetActualError(ex)}", 500);
            }
        }

        private async Task ClearDefaultAddresses(string accountId, DateTimeOffset updatedAt)
        {
            var addresses = await _authUow.CustomerAddresses.GetByAccountIdAsync(accountId);
            foreach (var existingAddress in addresses.Where(a => a.IsDefault))
            {
                existingAddress.IsDefault = false;
                existingAddress.UpdatedAt = updatedAt;
                _authUow.CustomerAddresses.Update(existingAddress);
            }
        }

        private static CustomerAddressResponse ToResponse(CustomerAddress address)
        {
            return new CustomerAddressResponse
            {
                Id = address.Id,
                AccountId = address.AccountId,
                ReceiverName = address.ReceiverName,
                ReceiverPhone = address.ReceiverPhone,
                Province = address.Province,
                District = address.District,
                Ward = address.Ward,
                StreetAddress = address.StreetAddress,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }

        private static ValidationResult? ValidateAccountAndAddress(string accountId, string? receiverName, string? receiverPhone, string? province, string? district, string? ward, string? streetAddress)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return new ValidationResult("Unauthorized.", 401);

            if (string.IsNullOrWhiteSpace(receiverName))
                return new ValidationResult("Receiver name is required.", 400);

            if (receiverName.Length > 100)
                return new ValidationResult("Receiver name must be 100 characters or fewer.", 400);

            if (string.IsNullOrWhiteSpace(receiverPhone))
                return new ValidationResult("Receiver phone is required.", 400);

            if (receiverPhone.Length > 20)
                return new ValidationResult("Receiver phone must be 20 characters or fewer.", 400);

            if (string.IsNullOrWhiteSpace(province))
                return new ValidationResult("Province is required.", 400);

            if (province.Length > 100)
                return new ValidationResult("Province must be 100 characters or fewer.", 400);

            if (string.IsNullOrWhiteSpace(district))
                return new ValidationResult("District is required.", 400);

            if (district.Length > 100)
                return new ValidationResult("District must be 100 characters or fewer.", 400);

            if (string.IsNullOrWhiteSpace(ward))
                return new ValidationResult("Ward is required.", 400);

            if (ward.Length > 100)
                return new ValidationResult("Ward must be 100 characters or fewer.", 400);

            if (string.IsNullOrWhiteSpace(streetAddress))
                return new ValidationResult("Street address is required.", 400);

            if (streetAddress.Length > 255)
                return new ValidationResult("Street address must be 255 characters or fewer.", 400);

            return null;
        }

        private static string GetActualError(Exception ex)
        {
            return ex.InnerException?.Message ?? ex.Message;
        }

        private sealed record ValidationResult(string Message, int StatusCode);
    }
}
