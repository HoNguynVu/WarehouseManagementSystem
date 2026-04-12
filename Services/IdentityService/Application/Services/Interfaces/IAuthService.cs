using Application.DTOs.Requests;
using Application.DTOs.Responses;
using SharedLibrary.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<string>> SignUpAsync(SignUpRequest request);
        Task<ApiResponse<string>> ResendOtpVerifyAsync(string accountId);
        Task<ApiResponse<string>> VerifyOtpAsync(OtpVerifyRequest request);
        Task<ApiResponse<string>> ResetPasswordRequestAsync(string email);
        Task<ApiResponse<string>> ResendOtpResetAsync(string accountId);
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request);
        Task<ApiResponse<string>> VerifyResetOtpAsync(OtpVerifyRequest request);
        Task<ApiResponse<SignInResponse>> SignInAsync(SignInRequest request);
        Task<ApiResponse<string>> SignOutAsync(string refreshToken);
    }
}
