using SolTechnology.Core.Guards.Context;

namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static IntContext Int(int parameter, string parameterName)
        {
            return new IntContext(parameter, parameterName);
        }

        public  static GuardsContext2 Int(int property, string propertyName, Action<IntContext> validate)
        {
            GuardsContext.Int(property, propertyName, validate);

            return GuardsContext;
        }

        public static GuardsContext2 Int(this GuardsContext2 context, int property, string propertyName, Action<IntContext> validate)
        {
            IntContext stringContext = new IntContext(property, propertyName);
            validate(stringContext);

            return context;
        }


        public static IntContext NotZero(this IntContext guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                GuardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static IntContext NotNegative(this IntContext guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                GuardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static IntContext NotPositive(this IntContext guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                GuardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static IntContext GreaterThan(this IntContext guardsContext, int toCompare)
        {
            if (!(guardsContext.Value > toCompare))
            {
                GuardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntContext GreaterEqual(this IntContext guardsContext, int toCompare)
        {
            if (!(guardsContext.Value >= toCompare))
            {
                GuardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntContext LessThan(this IntContext guardsContext, int toCompare)
        {
            if (!(guardsContext.Value < toCompare))
            {
                GuardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntContext LessEqual(this IntContext guardsContext, int toCompare)
        {
            if (!(guardsContext.Value <= toCompare))
            {
                GuardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntContext Equal(this IntContext guardsContext, int toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                GuardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntContext NotEqual(this IntContext guardsContext, int toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                GuardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntContext InRange(this IntContext guardsContext, int min, int max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                GuardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static IntContext NotInRange(this IntContext guardsContext, int min, int max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                GuardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}