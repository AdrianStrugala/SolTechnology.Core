using System.Collections.Generic;

namespace DreamTravel.DatabaseData.Query.GetPreviewUsers
{
    public interface IGetPreviewUsers
    {
        List<PreviewUser> Execute();
    }
}