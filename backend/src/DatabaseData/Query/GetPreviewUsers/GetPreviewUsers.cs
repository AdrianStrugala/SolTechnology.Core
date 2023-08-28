using System.Collections.Generic;
using DreamTravel.Domain.Users;

namespace DreamTravel.DatabaseData.Query.GetPreviewUsers
{
    public class GetPreviewUsers : IGetPreviewUsers
    {
        public List<PreviewUser> Execute()
        {
            List<PreviewUser> result = new List<PreviewUser>();

            result.Add(new PreviewUser
            {
                Email = "adistr94@gmail.com",
                Name = "Adi"
            });

            result.Add(new PreviewUser
            {
                Email = "alek-pawul@wp.pl",
                Name = "Alek"
            });

            result.Add(new PreviewUser
            {
                Email = "czechowski.priv@gmail.com",
                Name = "Andrzej"
            });

            result.Add(new PreviewUser
            {
                Email = "k.tobolski94@gmail.com",
                Name = "Krzysiu"
            });

            result.Add(new PreviewUser
            {
                Email = "karolina.brenzak@gmail.com",
                Name = "Karolina"
            });

            result.Add(new PreviewUser
            {
                Email = "lukasz.kamil.wojtczak@gmail.com",
                Name = "Łukasz"
            });

            result.Add(new PreviewUser
            {
                Email = "maria.chorazy94@gmail.com",
                Name = "Serduszko"
            });

            result.Add(new PreviewUser
            {
                Email = "mkmac231@gmail.com",
                Name = "Mac"
            });

            result.Add(new PreviewUser
            {
                Email = "struanna@o2.pl",
                Name = "Ania"
            });

            result.Add(new PreviewUser
            {
                Email = "szelus255@gmail.com",
                Name = "Szela"
            });

            result.Add(new PreviewUser
            {
                Email = "tomasz.a.zmuda@gmail.com",
                Name = "Tomek"
            });

            result.Add(new PreviewUser
            {
                Email = "zofia.natanek@gmail.com",
                Name = "Zosia"
            });

            result.Add(new PreviewUser
            {
                Email = "tomasz.walicki.1994@gmail.com",
                Name = "Walik"
            });

            return result;
        }
    }
}
