namespace ElsaServer.SuperChain.Errors;

public class Error
{
    public virtual string Message { get; set; } = null!;
    public string? Description { get; set; }
    public bool Recoverable { get; set; } = false;

    public static Error From(Exception exception) =>
        new()
        {
            Message = exception.Message,
            Description = exception.StackTrace
        };
}