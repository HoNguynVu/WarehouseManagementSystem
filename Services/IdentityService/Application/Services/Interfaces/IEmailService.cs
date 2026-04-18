using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmail(string email, string otp);
        Task SendPasswordResetEmail(string email, string otp);
    }
}
