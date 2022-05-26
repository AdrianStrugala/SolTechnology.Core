namespace SolTechnology.Core.Guards.Context;

public class GuardsException : AggregateException
{
    public GuardsException(IEnumerable<Exception> exceptions) : base(exceptions)
    {
            
    }
}