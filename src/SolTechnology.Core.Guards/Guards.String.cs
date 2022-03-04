namespace SolTechnology.Core.Guards
{
    public static partial class Guards
    {
        public static GuardsContext<string> String(string parameter, string parameterName)
        {
            return new GuardsContext<string>(parameter, parameterName);
        }

        public static GuardsContext<string> NotNull(this GuardsContext<string> guardsContext)
        {
            if (guardsContext.Value == null)
            {
                throw new ArgumentException($"String [{guardsContext.Name}] cannot be null!");
            }

            return guardsContext;
        }

        public static GuardsContext<string> NotEmpty(this GuardsContext<string> guardsContext)
        {
            if (guardsContext.Value == string.Empty)
            {
                throw new ArgumentException($"String [{guardsContext.Name}] cannot be empty!");
            }

            return guardsContext;
        }

        public static GuardsContext<string> NotWhitespace(this GuardsContext<string> guardsContext)
        {
            if (guardsContext.Value != null && string.IsNullOrWhiteSpace(guardsContext.Value))
            {
                throw new ArgumentException($"String [{guardsContext.Name}] cannot be whitespace!");
            }

            return guardsContext;
        }

        public static GuardsContext<string> Equal(this GuardsContext<string> guardsContext, string toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                throw new ArgumentException($"String [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<string> NotEqual(this GuardsContext<string> guardsContext, string toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                throw new ArgumentException($"String [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }
    }
}