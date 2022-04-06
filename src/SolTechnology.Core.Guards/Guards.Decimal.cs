namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static GuardsContext<decimal> Decimal(decimal parameter, string parameterName)
        {
            return new GuardsContext<decimal>(parameter, parameterName);
        }

        public static GuardsContext<decimal> NotZero(this GuardsContext<decimal> guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static GuardsContext<decimal> NotNegative(this GuardsContext<decimal> guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static GuardsContext<decimal> NotPositive(this GuardsContext<decimal> guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static GuardsContext<decimal> GreaterThan(this GuardsContext<decimal> guardsContext, decimal toCompare)
        {
            if (guardsContext.Value > toCompare) return guardsContext;
            throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");

        }

        public static GuardsContext<decimal> GreaterEqual(this GuardsContext<decimal> guardsContext, decimal toCompare)
        {
            if (guardsContext.Value >= toCompare) return guardsContext;
            throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");

        }

        public static GuardsContext<decimal> LessThan(this GuardsContext<decimal> guardsContext, decimal toCompare)
        {
            if (guardsContext.Value < toCompare) return guardsContext;
            throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");

        }

        public static GuardsContext<decimal> LessEqual(this GuardsContext<decimal> guardsContext, decimal toCompare)
        {
            if (guardsContext.Value <= toCompare) return guardsContext;
            throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");

        }

        public static GuardsContext<decimal> Equal(this GuardsContext<decimal> guardsContext, decimal toCompare)
        {
            if (guardsContext.Value == toCompare) return guardsContext;
            throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");

        }

        public static GuardsContext<decimal> NotEqual(this GuardsContext<decimal> guardsContext, decimal toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<decimal> InRange(this GuardsContext<decimal> guardsContext, decimal min, decimal max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<decimal> NotInRange(this GuardsContext<decimal> guardsContext, decimal min, decimal max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                throw new ArgumentException($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}