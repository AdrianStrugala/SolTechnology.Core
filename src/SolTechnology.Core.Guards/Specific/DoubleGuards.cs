namespace SolTechnology.Core.Guards
{
    public class DoubleGuards
    {
        internal double Value { get; set; }

        internal string Name { get; set; }

        internal List<string> Errors { get; set; }

        public DoubleGuards(double value, string name, List<string> errors)
        {
            Value = value;
            Name = name;
            Errors = errors;
        }
    }



    public static class DoubleGuardsExtensions
    {
        public static DoubleGuards NotZero(this DoubleGuards guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                guardsContext.Errors.Add($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static DoubleGuards NotNegative(this DoubleGuards guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                guardsContext.Errors.Add($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static DoubleGuards NotPositive(this DoubleGuards guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                guardsContext.Errors.Add($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static DoubleGuards GreaterThan(this DoubleGuards guardsContext, double toCompare)
        {
            if (!(guardsContext.Value > toCompare))
            {
                guardsContext.Errors.Add(
                    $"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DoubleGuards GreaterEqual(this DoubleGuards guardsContext, double toCompare)
        {
            if (!(guardsContext.Value >= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DoubleGuards LessThan(this DoubleGuards guardsContext, double toCompare)
        {
            if (!(guardsContext.Value < toCompare))
            {
                guardsContext.Errors.Add(
                    $"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DoubleGuards LessEqual(this DoubleGuards guardsContext, double toCompare)
        {
            if (!(guardsContext.Value <= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DoubleGuards Equal(this DoubleGuards guardsContext, double toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                guardsContext.Errors.Add($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DoubleGuards NotEqual(this DoubleGuards guardsContext, double toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                guardsContext.Errors.Add($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DoubleGuards InRange(this DoubleGuards guardsContext, double min, double max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                guardsContext.Errors.Add($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static DoubleGuards NotInRange(this DoubleGuards guardsContext, double min, double max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                guardsContext.Errors.Add($"Double [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}
