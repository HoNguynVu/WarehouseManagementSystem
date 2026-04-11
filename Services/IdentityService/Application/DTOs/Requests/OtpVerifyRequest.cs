using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class OtpVerifyRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
