namespace SolTechnology.Core.Guards;

public class GuardsException : AggregateException
{
    public GuardsException(IEnumerable<Exception> exceptions) : base(exceptions)
    {
            
    }
}