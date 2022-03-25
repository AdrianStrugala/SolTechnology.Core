namespace SolTechnology.Core.MessageBus;

public interface IMessage
{
    private static string _id;

    string Id
    {
        get
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString();
            }

            return _id;
        }
    }
}