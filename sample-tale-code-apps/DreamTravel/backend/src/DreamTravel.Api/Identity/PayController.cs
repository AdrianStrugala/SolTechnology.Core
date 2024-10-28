using System.Threading;
using System.Threading.Tasks;
using DreamTravel.Identity.Commands.Pay;
using DreamTravel.Identity.HttpClients.Aiia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Api.Identity
{
    public class PayController : Controller
    {
        private readonly ICommandHandler<PayCommand, CreatePaymentResponse> _payHandler;

        public PayController(ICommandHandler<PayCommand, CreatePaymentResponse> payHandler)
        {
            _payHandler = payHandler;
        }

        [HttpPost]
        [Route("api/users/pay")]
        public async Task<IActionResult> Pay([FromBody] PayCommand query)
        {
            var result = await _payHandler.Handle(query, CancellationToken.None);

            return Ok(result.Data);
        }

    }
}