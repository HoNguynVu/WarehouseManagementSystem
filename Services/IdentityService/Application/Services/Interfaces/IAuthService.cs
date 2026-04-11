using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using SharedLibrary.Responses;

namespace Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<string>> SignUpAsync(SignUpRequest request);
    }
}
