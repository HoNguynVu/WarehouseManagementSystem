using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class SignInResponse
    {
        public string AccountId { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessToken {  get; set; } = string.Empty;
    }
}
