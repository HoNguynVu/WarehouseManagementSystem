using Application.DTOs.Requests;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            var result = await _authService.SignUpAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
        {
            var result = await _authService.SignInAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("signout")]
        public async Task<IActionResult> SignOut([FromQuery] string refreshToken)
        {
            var result = await _authService.SignOutAsync(refreshToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromQuery] string accountId)
        {
            var result = await _authService.ResendOtpVerifyAsync(accountId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reset-password-request")]
        public async Task<IActionResult> ResetPasswordRequest([FromQuery] string email)
        {
            var result = await _authService.ResetPasswordRequestAsync(email);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("verify-reset-otp")]
        public async Task<IActionResult> VerifyResetOtp([FromBody] OtpVerifyRequest request)
        {
            var result = await _authService.VerifyResetOtpAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("resend-reset-otp")]
        public async Task<IActionResult> ResendResetOtp([FromQuery] string accountId)
        {
            var result = await _authService.ResendOtpResetAsync(accountId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            return StatusCode(result.StatusCode, result);
        }
    }
}
