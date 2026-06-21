using Application.DTOs;
using Application.Features.Payments.Commands.ProcessPaymentCallback;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] ZaloPayCallbackDTO cbdata)
        {
            var result = await _mediator.Send(new ProcessPaymentCallbackCommand { Cbdata = cbdata });

            if (result)
                return Ok(new { return_code = 1, return_message = "success" });

            // Still return 200 with fail code so ZaloPay won't retry unrecoverable errors (e.g. invalid MAC)
            return Ok(new { return_code = 0, return_message = "fail" });
        }

        [HttpPost("{orderId}/mock-payment")]
        public async Task<IActionResult> MockPayment(string orderId)
        {
            var result = await _mediator.Send(new Application.Features.Payments.Commands.MockPayment.MockPaymentCommand { OrderId = orderId });
            return StatusCode(result.StatusCode, result);
        }
    }
}
