namespace ElsaServer.SuperChain;

public class ChainContext<TInput, TOutput>
{
    public TInput Input { get; set; } = default!;
    public TOutput Output { get; set; } = default!;
}