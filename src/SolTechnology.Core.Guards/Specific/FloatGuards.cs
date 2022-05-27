namespace SolTechnology.Core.Guards
{
    public class FloatGuards
    {
        internal float Value { get; set; }

        internal string Name { get; set; }

        internal List<string> Errors { get; set; }

        public FloatGuards(float value, string name, List<string> errors)
        {
            Value = value;
            Name = name;
            Errors = errors;
        }
    }



    public static class FloatGuardsExtensions
    {
        public static FloatGuards NotZero(this FloatGuards guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                guardsContext.Errors.Add($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static FloatGuards NotNegative(this FloatGuards guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                guardsContext.Errors.Add($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static FloatGuards NotPositive(this FloatGuards guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                guardsContext.Errors.Add($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static FloatGuards GreaterThan(this FloatGuards guardsContext, float toCompare)
        {
            if (!(guardsContext.Value > toCompare))
            {
                guardsContext.Errors.Add(
                    $"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static FloatGuards GreaterEqual(this FloatGuards guardsContext, float toCompare)
        {
            if (!(guardsContext.Value >= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static FloatGuards LessThan(this FloatGuards guardsContext, float toCompare)
        {
            if (!(guardsContext.Value < toCompare))
            {
                guardsContext.Errors.Add(
                    $"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static FloatGuards LessEqual(this FloatGuards guardsContext, float toCompare)
        {
            if (!(guardsContext.Value <= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static FloatGuards Equal(this FloatGuards guardsContext, float toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                guardsContext.Errors.Add($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static FloatGuards NotEqual(this FloatGuards guardsContext, float toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                guardsContext.Errors.Add($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static FloatGuards InRange(this FloatGuards guardsContext, float min, float max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                guardsContext.Errors.Add($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static FloatGuards NotInRange(this FloatGuards guardsContext, float min, float max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                guardsContext.Errors.Add($"Float [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}
