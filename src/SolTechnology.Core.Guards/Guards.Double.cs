namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static GuardsContext<double> Double(double parameter, string parameterName)
        {
            return new GuardsContext<double>(parameter, parameterName);
        }

        public static GuardsContext<double> NotZero(this GuardsContext<double> guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static GuardsContext<double> NotNegative(this GuardsContext<double> guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static GuardsContext<double> NotPositive(this GuardsContext<double> guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static GuardsContext<double> GreaterThan(this GuardsContext<double> guardsContext, double toCompare)
        {
            if (guardsContext.Value > toCompare) return guardsContext;
            throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");

        }

        public static GuardsContext<double> GreaterEqual(this GuardsContext<double> guardsContext, double toCompare)
        {
            if (guardsContext.Value >= toCompare) return guardsContext;
            throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");

        }

        public static GuardsContext<double> LessThan(this GuardsContext<double> guardsContext, double toCompare)
        {
            if (guardsContext.Value < toCompare) return guardsContext;
            throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");

        }

        public static GuardsContext<double> LessEqual(this GuardsContext<double> guardsContext, double toCompare)
        {
            if (guardsContext.Value <= toCompare) return guardsContext;
            throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");

        }

        public static GuardsContext<double> Equal(this GuardsContext<double> guardsContext, double toCompare)
        {
            if (guardsContext.Value == toCompare) return guardsContext;
            throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");

        }

        public static GuardsContext<double> NotEqual(this GuardsContext<double> guardsContext, double toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<double> InRange(this GuardsContext<double> guardsContext, double min, double max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<double> NotInRange(this GuardsContext<double> guardsContext, double min, double max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                throw new ArgumentException($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}