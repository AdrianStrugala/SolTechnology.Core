namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static GuardsContext<long> Long(long parameter, string parameterName)
        {
            return new GuardsContext<long>(parameter, parameterName);
        }

        public static GuardsContext<long> NotZero(this GuardsContext<long> guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static GuardsContext<long> NotNegative(this GuardsContext<long> guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static GuardsContext<long> NotPositive(this GuardsContext<long> guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static GuardsContext<long> GreaterThan(this GuardsContext<long> guardsContext, long toCompare)
        {
            if (guardsContext.Value > toCompare) return guardsContext;
            throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");

        }

        public static GuardsContext<long> GreaterEqual(this GuardsContext<long> guardsContext, long toCompare)
        {
            if (guardsContext.Value >= toCompare) return guardsContext;
            throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");

        }

        public static GuardsContext<long> LessThan(this GuardsContext<long> guardsContext, long toCompare)
        {
            if (guardsContext.Value < toCompare) return guardsContext;
            throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");

        }

        public static GuardsContext<long> LessEqual(this GuardsContext<long> guardsContext, long toCompare)
        {
            if (guardsContext.Value <= toCompare) return guardsContext;
            throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");

        }

        public static GuardsContext<long> Equal(this GuardsContext<long> guardsContext, long toCompare)
        {
            if (guardsContext.Value == toCompare) return guardsContext;
            throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");

        }

        public static GuardsContext<long> NotEqual(this GuardsContext<long> guardsContext, long toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<long> InRange(this GuardsContext<long> guardsContext, long min, long max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<long> NotInRange(this GuardsContext<long> guardsContext, long min, long max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                throw new ArgumentException($"long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}