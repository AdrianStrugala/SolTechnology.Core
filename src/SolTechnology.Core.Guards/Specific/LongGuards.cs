namespace SolTechnology.Core.Guards
{
    public class LongGuards
    {
        internal long Value { get; set; }

        internal string Name { get; set; }

        internal List<string> Errors { get; set; }

        public LongGuards(long value, string name, List<string> errors)
        {
            Value = value;
            Name = name;
            Errors = errors;
        }
    }



    public static class LongGuardsExtensions
    {
        public static LongGuards NotZero(this LongGuards guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                guardsContext.Errors.Add($"Long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static LongGuards NotNegative(this LongGuards guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                guardsContext.Errors.Add($"Long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static LongGuards NotPositive(this LongGuards guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                guardsContext.Errors.Add($"Long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static LongGuards GreaterThan(this LongGuards guardsContext, long toCompare)
        {
            if (!(guardsContext.Value > toCompare))
            {
                guardsContext.Errors.Add(
                    $"Long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static LongGuards GreaterEqual(this LongGuards guardsContext, long toCompare)
        {
            if (!(guardsContext.Value >= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static LongGuards LessThan(this LongGuards guardsContext, long toCompare)
        {
            if (!(guardsContext.Value < toCompare))
            {
                guardsContext.Errors.Add(
                    $"Long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static LongGuards LessEqual(this LongGuards guardsContext, long toCompare)
        {
            if (!(guardsContext.Value <= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static LongGuards Equal(this LongGuards guardsContext, long toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                guardsContext.Errors.Add($"Long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static LongGuards NotEqual(this LongGuards guardsContext, long toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                guardsContext.Errors.Add($"Long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static LongGuards InRange(this LongGuards guardsContext, long min, long max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                guardsContext.Errors.Add($"Long [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static LongGuards NotInRange(this LongGuards guardsContext, long min, long max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                guardsContext.Errors.Add($"Long [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}
