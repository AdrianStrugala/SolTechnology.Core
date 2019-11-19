using System.Collections.Generic;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.Users
{
    public partial class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public UserRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }


        public List<User> GetPreviewUsers()
        {
            List<User> result = new List<User>();

            result.Add(new User
            {
                Email = "adistr94@gmail.com",
                Name = "Adi"
            });

            result.Add(new User
            {
                Email = "alek-pawul@wp.pl",
                Name = "Alek"
            });

            result.Add(new User
            {
                Email = "czechowski.priv@gmail.com",
                Name = "Andrzej"
            });

            result.Add(new User
            {
                Email = "k.tobolski94@gmail.com",
                Name = "Krzysiu"
            });

            result.Add(new User
            {
                Email = "karolina.brenzak@gmail.com",
                Name = "Karolina"
            });

            result.Add(new User
            {
                Email = "katada9707@wp.pl",
                Name = "Kasiek"
            });

            result.Add(new User
            {
                Email = "lukasz.kamil.wojtczak@gmail.com",
                Name = "Łukasz"
            });

            result.Add(new User
            {
                Email = "maria.chorazy94@gmail.com",
                Name = "Serduszko"
            });

            result.Add(new User
            {
                Email = "mkmac231@gmail.com",
                Name = "Mac"
            });

            result.Add(new User
            {
                Email = "struanna@o2.pl",
                Name = "Ania"
            });

            result.Add(new User
            {
                Email = "szelus255@gmail.com",
                Name = "Szela"
            });

            result.Add(new User
            {
                Email = "tomasz.a.zmuda@gmail.com",
                Name = "Tomek"
            });

            result.Add(new User
            {
                Email = "zofia.natanek@gmail.com",
                Name = "Zosia"
            });

            result.Add(new User
            {
                Email = "tomasz.walicki.1994@gmail.com",
                Name = "Walik"
            });

            return result;
        }
    }
}
