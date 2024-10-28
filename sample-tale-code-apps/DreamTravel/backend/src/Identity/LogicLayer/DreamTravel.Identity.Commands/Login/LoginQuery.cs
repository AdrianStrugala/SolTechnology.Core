using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.Login
{
    public class LoginQuery : IRequest<Result<LoginResult>>
    {
        public string Password { get; set; }
        public string Email { get; set; }
    }
}
