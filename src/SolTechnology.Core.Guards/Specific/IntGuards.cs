namespace SolTechnology.Core.Guards
{
    public class IntGuards
    {
        internal int Value { get; set; }

        internal string Name { get; set; }

        internal List<string> Errors { get; set; }

        public IntGuards(int value, string name, List<string> errors)
        {
            Value = value;
            Name = name;
            Errors = errors;
        }
    }



    public static class IntGuardsExtensions
    {
        public static IntGuards NotZero(this IntGuards guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                guardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static IntGuards NotNegative(this IntGuards guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                guardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static IntGuards NotPositive(this IntGuards guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                guardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static IntGuards GreaterThan(this IntGuards guardsContext, int toCompare)
        {
            if (!(guardsContext.Value > toCompare))
            {
                guardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntGuards GreaterEqual(this IntGuards guardsContext, int toCompare)
        {
            if (!(guardsContext.Value >= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntGuards LessThan(this IntGuards guardsContext, int toCompare)
        {
            if (!(guardsContext.Value < toCompare))
            {
                guardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntGuards LessEqual(this IntGuards guardsContext, int toCompare)
        {
            if (!(guardsContext.Value <= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntGuards Equal(this IntGuards guardsContext, int toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                guardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntGuards NotEqual(this IntGuards guardsContext, int toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                guardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static IntGuards InRange(this IntGuards guardsContext, int min, int max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                guardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static IntGuards NotInRange(this IntGuards guardsContext, int min, int max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                guardsContext.Errors.Add($"Int [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}
