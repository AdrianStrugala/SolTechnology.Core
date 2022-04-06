namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static GuardsContext<float> Float(float parameter, string parameterName)
        {
            return new GuardsContext<float>(parameter, parameterName);
        }

        public static GuardsContext<float> NotZero(this GuardsContext<float> guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static GuardsContext<float> NotNegative(this GuardsContext<float> guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static GuardsContext<float> NotPositive(this GuardsContext<float> guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static GuardsContext<float> GreaterThan(this GuardsContext<float> guardsContext, float toCompare)
        {
            if (guardsContext.Value > toCompare) return guardsContext;
            throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");

        }

        public static GuardsContext<float> GreaterEqual(this GuardsContext<float> guardsContext, float toCompare)
        {
            if (guardsContext.Value >= toCompare) return guardsContext;
            throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");

        }

        public static GuardsContext<float> LessThan(this GuardsContext<float> guardsContext, float toCompare)
        {
            if (guardsContext.Value < toCompare) return guardsContext;
            throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");

        }

        public static GuardsContext<float> LessEqual(this GuardsContext<float> guardsContext, float toCompare)
        {
            if (guardsContext.Value <= toCompare) return guardsContext;
            throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");

        }

        public static GuardsContext<float> Equal(this GuardsContext<float> guardsContext, float toCompare)
        {
            if (guardsContext.Value == toCompare) return guardsContext;
            throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");

        }

        public static GuardsContext<float> NotEqual(this GuardsContext<float> guardsContext, float toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<float> InRange(this GuardsContext<float> guardsContext, float min, float max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static GuardsContext<float> NotInRange(this GuardsContext<float> guardsContext, float min, float max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                throw new ArgumentException($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}