using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] ZaloPayCallbackDTO cbdata)
        {
            var result = await _paymentService.ProcessCallback(cbdata);
            
            if (result)
            {
                return Ok(new { return_code = 1, return_message = "success" });
            }
            
            // If failed due to MAC or other issues, still return 200 with fail code to ZaloPay
            // so they won't retry excessively if it's an unrecoverable logic error (like invalid MAC).
            return Ok(new { return_code = 0, return_message = "fail" });
        }
    }
}
