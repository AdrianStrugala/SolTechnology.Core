using SolTechnology.Core.Guards.Context;

namespace SolTechnology.Core.Guards
{
    public static partial class Guards
    {
        public static GuardsContext2 String(string property, string propertyName, Action<StringContext> validate)
        {
            StringContext stringContext = new StringContext(property, propertyName);
            validate(stringContext);

            return GuardsContext;
        }

        // public static GuardsContext2 String(this GuardsContext2 context, string property, string propertyName, Action<StringContext> validate)
        // {
        //     StringContext stringContext = new StringContext(property, propertyName);
        //     validate(stringContext);
        //
        //     return GuardsContext;
        // }


        public static StringContext NotNull(this StringContext context)
        {
            if (context.Value == null)
            {
                GuardsContext.Errors.Add($"String [{context.Name}] cannot be null!");
            }

            return context;
        }


        public static StringContext NotEmpty(this StringContext guardsContext)
        {
            if (guardsContext.Value == string.Empty)
            {
                GuardsContext.Errors.Add($"String [{guardsContext.Name}] cannot be empty!");
            }
        
            return guardsContext;
        }
        
        public static StringContext NotWhitespace(this StringContext guardsContext)
        {
            if (guardsContext.Value != null && string.IsNullOrWhiteSpace(guardsContext.Value))
            {
                GuardsContext.Errors.Add($"String [{guardsContext.Name}] cannot be whitespace!");
            }
        
            return guardsContext;
        }
        
        public static StringContext Equal(this StringContext guardsContext, string toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                GuardsContext.Errors.Add($"String [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }
        
            return guardsContext;
        }
        
        public static StringContext NotEqual(this StringContext guardsContext, string toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                GuardsContext.Errors.Add($"String [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }
        
            return guardsContext;
        }
    }
}