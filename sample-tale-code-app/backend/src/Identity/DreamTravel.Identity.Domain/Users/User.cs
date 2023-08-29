using System;
using Guards;

namespace DreamTravel.Identity.Domain.Users
{
    public record User : AbstractEntity
    {
        public Guid UserId { get; }
        public string Name { get; init; }
        public string Password { get; private set; }
        public string Email { get; init; }


        private User()
        {
            
        }

        public User(string name, string password, string email)
        {
            Guard.ArgumentNotNullOrEmpty(name, nameof(name));
            Guard.ArgumentNotNullOrEmpty(password, nameof(password));
            Guard.ArgumentNotNullOrEmpty(email, nameof(email));

            Name = name;
            Password = password;
            Email = email;

            UserId = Guid.NewGuid();
        }

        public void UpdatePassword(string password)
        {
            Guard.ArgumentNotNullOrEmpty(password, nameof(password));
            Password = password;
        }
    }
}
