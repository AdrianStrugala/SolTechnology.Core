namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static GuardsContext<int> Int(int parameter, string parameterName)
        {
            return new GuardsContext<int>(parameter, parameterName);
        }

        public static GuardsContext<int> NotZero(this GuardsContext<int> guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                throw new ArgumentException($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static GuardsContext<int> NotNegative(this GuardsContext<int> guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                throw new ArgumentException($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static GuardsContext<int> NotPositive(this GuardsContext<int> guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                throw new ArgumentException($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static GuardsContext<int> Equal(this GuardsContext<int> guardsContext, int toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                throw new ArgumentException($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<int> NotEqual(this GuardsContext<int> guardsContext, int toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                throw new ArgumentException($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<int> InRange(this GuardsContext<int> guardsContext, int min, int max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                throw new ArgumentException($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<int> NotInRange(this GuardsContext<int> guardsContext, int min, int max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                throw new ArgumentException($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}