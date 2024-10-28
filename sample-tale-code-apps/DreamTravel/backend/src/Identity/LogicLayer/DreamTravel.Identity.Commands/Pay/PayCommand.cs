using System;
using DreamTravel.Identity.HttpClients.Aiia;
using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.Pay
{
    public class PayCommand : IRequest<Result<CreatePaymentResponse>>
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
