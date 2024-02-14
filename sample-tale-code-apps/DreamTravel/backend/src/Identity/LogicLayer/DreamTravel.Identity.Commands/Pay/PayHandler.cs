using System.Threading.Tasks;
using DreamTravel.Identity.DatabaseData.Repositories.Users;
using DreamTravel.Identity.HttpClients.Aiia;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.Pay
{
    public class PayHandler : ICommandHandler<PayCommand, CreatePaymentResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAiiaApi _api;

        public PayHandler(IUserRepository userRepository, IAiiaApi aiiaApi)
        {
            _userRepository = userRepository;
            _api = aiiaApi;
        }

        public async Task<ResultBase<CreatePaymentResponse>> Handle(PayCommand command)
        {
            var paymentRequest = new CreateAcceptPaymentRequest
            {
                amount = decimal.Round(command.Amount),
                currency = command.Currency,
                schemeId = "DanishDomesticCreditTransfer",
                referece = "1234567890",
                destinationId = "d9509516-f5d9-408f-9eec-6689b3bde458",
                redirectUrl = "http://localhost:4200/callback",
                preselectedCountry = "DK"
            };

            var result = await _api.CreateAcceptPayment(paymentRequest);

            return ResultBase<CreatePaymentResponse>.Succeeded(result);
        }
    }
}