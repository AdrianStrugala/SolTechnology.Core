namespace SolTechnology.Core.Guards.Context
{
    public class GuardsContext2
    {
        public List<string> Errors;

        public GuardsContext2()
        {
            Errors = new List<string>();
        }

        public void ThrowOnErrors()
        {
            throw new AggregateException(Errors.Select(e => new ArgumentException(e)));
        }
    }
}
