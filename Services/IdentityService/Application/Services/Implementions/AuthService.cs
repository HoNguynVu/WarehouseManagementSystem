using Application.DTOs.Requests;
using Application.DTOs.Responses;
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
        private readonly JwtGenerator _jwtGenerator;
        private readonly IAuthUow _authUow;

        public AuthService(IEmailService emailService, IAuthUow authUow, JwtGenerator jwtGenerator)
        {
            _emailService = emailService;
            _authUow = authUow;
            _jwtGenerator = jwtGenerator;
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

        public async Task<ApiResponse<string>> ResendOtpVerifyAsync(string accountId)
        {
            if (accountId == null)
            {
                return ApiResponse<string>.Failure("Account ID is required.", 404);
            }
            var account = await _authUow.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                return ApiResponse<string>.Failure("Account not found.", 404);
            }

            var otp = new Otps
            {
                Id = IdGenerator.GenerateId(),
                Code = AuthHelpers.GenerateOTP(),
                Purpose = OtpPurposes.EmailVerification,
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow,
                AccountId = account.Id
            };

            try
            {
                await _authUow.BeginTransactionAsync();
                _authUow.Otps.Create(otp);
                await _authUow.CommitAsync();
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}", 500);
            }

            try
            {
                await _emailService.SendVerificationEmail(account.Email, otp.Code);
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Failed to send verification email: {ex.Message}", 500);
            }

            return ApiResponse<string>.Success(account.Id, "OTP resent successfully. Please check your email.", 200);
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

        public async Task<ApiResponse<string>> ResetPasswordRequestAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return ApiResponse<string>.Failure("Email is required.", 404);
            }

            var account = await _authUow.Accounts.GetByEmailAsync(email);

            if (account == null)
            {
                return ApiResponse<string>.Failure("Account not found.", 404);
            }

            if (account.Status == AccountStatus.EmailUnverified || account.Status == AccountStatus.Inactive)
            {
                return ApiResponse<string>.Failure("Account is not active. Please verify your email first.", 403);
            }

            if (account.Status == AccountStatus.Suspended)
            {
                return ApiResponse<string>.Failure("Account is suspended. Please contact support.", 403);
            }

            var otp = new Otps
            {
                Id = IdGenerator.GenerateId(),
                Code = AuthHelpers.GenerateOTP(),
                Purpose = OtpPurposes.PasswordReset,
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow,
                AccountId = account.Id
            };

            try
            {
                await _authUow.BeginTransactionAsync();
                _authUow.Otps.Create(otp);
                await _authUow.CommitAsync();
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}", 500);
            }

            try
            {
                await _emailService.SendPasswordResetEmail(account.Email, otp.Code);
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Failed to send password reset email: {ex.Message}", 500);
            }

            return ApiResponse<string>.Success(account.Id, "Password reset email sent successfully. Please check your email.", 200);
        }

        public async Task<ApiResponse<string>> ResendOtpResetAsync(string accountId)
        {
            if (accountId == null)
            {
                return ApiResponse<string>.Failure("Account ID is required.", 404);
            }
            var account = await _authUow.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                return ApiResponse<string>.Failure("Account not found.", 404);
            }

            var otp = new Otps
            {
                Id = IdGenerator.GenerateId(),
                Code = AuthHelpers.GenerateOTP(),
                Purpose = OtpPurposes.PasswordReset,
                ExpirationTime = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow,
                AccountId = account.Id
            };

            try
            {
                await _authUow.BeginTransactionAsync();
                _authUow.Otps.Create(otp);
                await _authUow.CommitAsync();
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}", 500);
            }

            try
            {
                await _emailService.SendVerificationEmail(account.Email, otp.Code);
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Failed to send password reset email: {ex.Message}", 500);
            }

            return ApiResponse<string>.Success(account.Id, "OTP resent successfully. Please check your email.", 200);
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
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

            var newPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            account.Password = newPassword;

            try
            {
                await _authUow.BeginTransactionAsync();
                _authUow.Accounts.Update(account);
                await _authUow.CommitAsync();
                return ApiResponse<string>.Success(account.Id, "Password reset successfully.", 200);
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<string>> VerifyResetOtpAsync(OtpVerifyRequest request)
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
            var otp = await _authUow.Otps.GetByAccountIdAndPurposeAsync(account.Id, OtpPurposes.PasswordReset);
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
                return ApiResponse<string>.Success(account.Id, "OTP verified successfully.", 200);

            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<SignInResponse>> SignInAsync(SignInRequest request)
        {
            if (request == null)
            {
                return ApiResponse<SignInResponse>.Failure("Please enter Email and Password", 400);
            }

            var account = await _authUow.Accounts.GetByEmailAsync(request.Email);
            if (account == null)
            {
                return ApiResponse<SignInResponse>.Failure("No account be created by this email, please try another email or sign up", 404);
            }

            bool valid = BCrypt.Net.BCrypt.Verify(request.Password, account.Password);
            if (!valid)
            {
                return ApiResponse<SignInResponse>.Failure("Incorrect password, please try again", 401);
            }

            var refreshToken = AuthHelpers.CreateRefreshToken(account.Id);

            try
            {
                await _authUow.BeginTransactionAsync();
                _authUow.RefreshTokens.Create(refreshToken);
                await _authUow.CommitAsync();
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<SignInResponse>.Failure($"Database error: {ex.Message}", 500);
            }

            var accessToken = _jwtGenerator.GenerateToken(account);

            var response = new SignInResponse
            {
                AccountId = account.Id,
                RefreshToken = refreshToken.Token,
                AccessToken = accessToken
            };

            return ApiResponse<SignInResponse>.Success(response, "Sign in successful.", 200);
        }

        public async Task<ApiResponse<string>> SignOutAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return ApiResponse<string>.Failure("Refresh token is required.", 400);
            }

            var token = await _authUow.RefreshTokens.GetByTokenAsync(refreshToken);
            if (token == null)
            {
                return ApiResponse<string>.Failure("Invalid refresh token.", 404);
            }

            token.IsActive = false;
            token.RevokedAt = DateTime.UtcNow;
            try
            {
                await _authUow.BeginTransactionAsync();
                _authUow.RefreshTokens.Update(token);
                await _authUow.CommitAsync();
                return ApiResponse<string>.Success(token.AccountId, "Sign out successful.", 200);
            }
            catch (Exception ex)
            {
                await _authUow.RollbackAsync();
                return ApiResponse<string>.Failure($"Database error: {ex.Message}", 500);
            }
        }
    }
}
