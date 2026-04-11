using Application.DTOs;
using Application.Helpers;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infracstructure.UnitOfWorks;
using SharedLibrary.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementions
{
    public class AuthService : IAuthService
    {
        private readonly IEmailService _emailService;
        private readonly IAuthUow _authUow;

        public AuthService(IEmailService emailService, IAuthUow authUow)
        {
            _emailService = emailService;
            _authUow = authUow;
        }

        public async Task<ApiResponse<string>> SignUpAsync(SignUpRequest request)
        {
            if (request == null)
            {
                return ApiResponse<string>.Failure("Invalid request.");
            }

            if (string.IsNullOrEmpty(request.Username))
            {
                return ApiResponse<string>.Failure("Username is required.");
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                return ApiResponse<string>.Failure("Password is required.");
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                var old_account = await _authUow.Accounts.GetByEmailAsync(request.Email);
                if (old_account != null)
                {
                    if (old_account.Status == AccountStatus.Active)
                    {
                        return ApiResponse<string>.Failure("An account with this email already exists.", 409);
                    }
                    else if (old_account.Status == AccountStatus.Inactive || old_account.Status == AccountStatus.EmailUnverified)
                    {
                        return ApiResponse<string>.Failure("An account with this email already exists but is not active. Please recheck email.", 409);
                    }
                    else if (true)
                    {
                        return ApiResponse<string>.Failure("An account with this email already exists but is suspended. Please contact support.", 403);
                    }
                }
            }
            else
            {
                return ApiResponse<string>.Failure("Email is required.", 500);
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var new_account = new Accounts
            {
                Id = IdGenerator.GenerateId(),
                Username = request.Username,
                Email = request.Email,
                Password = passwordHash,
                Phone = request.Phone,
                Role = AccountRoles.User,
                Status = AccountStatus.EmailUnverified,
                CreatedAt = DateTime.UtcNow
            };

            var otp_code = new Otps
            {
                Id = IdGenerator.GenerateId(),
                Code = AuthHelpers.GenerateOTP(),
                AccountId = new_account.Id,
                Purpose = OtpPurposes.EmailVerification,
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow
            };

            await _authUow.BeginTransactionAsync();
            try
            {
                _authUow.Accounts.Create(new_account);
                _authUow.Otps.Create(otp_code);
                await _authUow.CommitAsync();
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}");
            }

            try
            {
                await _emailService.SendVerificationEmail(new_account.Email, otp_code.Code);
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Success(new_account.Id, "Account created, but failed to send verification email. Please request a new OTP.", 201);
            }

            return ApiResponse<string>.Success(new_account.Id, "Account created successfully. Please check your email.", 201);
        }

        public async Task<ApiResponse<string>> VerifyOtpAsync(OtpVerifyRequest request)
        {
            if (request == null)
            {
                return ApiResponse<string>.Failure("Invalid request.");
            }

            var account = await _authUow.Accounts.GetByIdAsync(request.AccountId);
            if (account == null)
            {
                return ApiResponse<string>.Failure("Account not found.", 404);
            }
            var otp = await _authUow.Otps.GetByAccountIdAndPurposeAsync(account.Id, OtpPurposes.EmailVerification);
            if (otp == null || otp.Code != request.Otp || otp.ExpirationTime < DateTime.UtcNow)
            {
                return ApiResponse<string>.Failure("Invalid or expired OTP.", 400);
            }

            otp.IsActive = false;
            account.Status = AccountStatus.Active;
            try
            {
                await _authUow.BeginTransactionAsync();
                _authUow.Otps.Update(otp);
                _authUow.Accounts.Update(account);
                await _authUow.CommitAsync();
                return ApiResponse<string>.Success(account.Id, "Email verified successfully.", 200);

            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}", 500);
            }
        }
    }
}
