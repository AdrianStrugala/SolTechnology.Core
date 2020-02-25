using System.Collections.Generic;
using DreamTravel.Domain.Users;

namespace DreamTravel.DatabaseData.Query.GetPreviewUsers
{
    public interface IGetPreviewUsers
    {
        List<User> Execute();
    }
}