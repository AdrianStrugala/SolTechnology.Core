
using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Identity.Commands.Register
{
    public class RegisterUserCommand : IRequest, IRequest<Result>
    {
        public string Name { get; init; }
        public string Password { get; init; }
        public string Email { get; init; }

        public RegisterUserCommand(string name, string password, string email)
        {
            Name = name;
            Password = password;
            Email = email;
        }
    }
}
