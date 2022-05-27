namespace SolTechnology.Core.Guards
{
    public class DecimalGuards
    {
        internal decimal Value { get; set; }

        internal string Name { get; set; }

        internal List<string> Errors { get; set; }

        public DecimalGuards(decimal value, string name, List<string> errors)
        {
            Value = value;
            Name = name;
            Errors = errors;
        }
    }



    public static class DecimalGuardsExtensions
    {
        public static DecimalGuards NotZero(this DecimalGuards guardsContext)
        {
            if (guardsContext.Value == 0)
            {
                guardsContext.Errors.Add($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be zero!");
            }

            return guardsContext;
        }

        public static DecimalGuards NotNegative(this DecimalGuards guardsContext)
        {
            if (guardsContext.Value < 0)
            {
                guardsContext.Errors.Add($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be negative!");
            }

            return guardsContext;
        }

        public static DecimalGuards NotPositive(this DecimalGuards guardsContext)
        {
            if (guardsContext.Value > 0)
            {
                guardsContext.Errors.Add($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be positive!");
            }

            return guardsContext;
        }

        public static DecimalGuards GreaterThan(this DecimalGuards guardsContext, decimal toCompare)
        {
            if (!(guardsContext.Value > toCompare))
            {
                guardsContext.Errors.Add(
                    $"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DecimalGuards GreaterEqual(this DecimalGuards guardsContext, decimal toCompare)
        {
            if (!(guardsContext.Value >= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be greater or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DecimalGuards LessThan(this DecimalGuards guardsContext, decimal toCompare)
        {
            if (!(guardsContext.Value < toCompare))
            {
                guardsContext.Errors.Add(
                    $"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser than [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DecimalGuards LessEqual(this DecimalGuards guardsContext, decimal toCompare)
        {
            if (!(guardsContext.Value <= toCompare))
            {
                guardsContext.Errors.Add(
                    $"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be lesser or equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DecimalGuards Equal(this DecimalGuards guardsContext, decimal toCompare)
        {
            if (guardsContext.Value != toCompare)
            {
                guardsContext.Errors.Add($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DecimalGuards NotEqual(this DecimalGuards guardsContext, decimal toCompare)
        {
            if (guardsContext.Value == toCompare)
            {
                guardsContext.Errors.Add($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be equal [{toCompare}]!");
            }

            return guardsContext;
        }

        public static DecimalGuards InRange(this DecimalGuards guardsContext, decimal min, decimal max)
        {
            if (guardsContext.Value < min || guardsContext.Value > max)
            {
                guardsContext.Errors.Add($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] has to be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }

        public static DecimalGuards NotInRange(this DecimalGuards guardsContext, decimal min, decimal max)
        {
            if (guardsContext.Value >= min && guardsContext.Value <= max)
            {
                guardsContext.Errors.Add($"Decimal [{guardsContext.Value}] of name [{guardsContext.Name}] cannot be in range: [{min}] to [{max}]!");
            }

            return guardsContext;
        }
    }
}
