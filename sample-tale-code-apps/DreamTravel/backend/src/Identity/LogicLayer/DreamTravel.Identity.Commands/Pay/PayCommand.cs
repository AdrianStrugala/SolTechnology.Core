using System;

namespace DreamTravel.Identity.Commands.Pay
{
    public class PayCommand
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
