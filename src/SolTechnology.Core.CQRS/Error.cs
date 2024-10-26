namespace SolTechnology.Core.CQRS
{
    public class Error
    {
        public string Message { get; set; }
        public string Description { get; set; }
        public bool Recoverable { get; set; } = false;

        public static Error From(Exception exception) =>
            new()
            {
                Message = exception.Message,
                Description = exception.StackTrace
            };
    }
}
