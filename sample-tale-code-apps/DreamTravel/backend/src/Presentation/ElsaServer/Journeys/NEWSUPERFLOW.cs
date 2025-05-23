using ElsaServer.SuperChain;

namespace ElsaServer.Journeys;

public class NEWSUPERFLOW(IServiceProvider serviceProvider) : ChainHandler<NewQuery, NewContext, NewResult>(serviceProvider)
{
    protected override async Task HandleChain()
    {
        await Invoke<Login>();
    }
}

public class Login : SuperChain.IChainStep<NewContext>
{
    public Task<Result> Execute(NewContext context)
    {
        throw new NotImplementedException();
    }
}

public class NewResult
{
}

public class NewContext : ChainContext<NewQuery, NewResult>
{
}

public class NewQuery
{
}