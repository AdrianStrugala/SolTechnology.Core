public class PipelineContext<TInput, TOutput>
{
    public TInput Input { get; set; } = default!;
    public TOutput Output { get; set; } = default!;
}