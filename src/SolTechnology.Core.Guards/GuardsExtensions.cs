namespace SolTechnology.Core.Guards;

public static class GuardsExtensions
{
    public static Guards String(this Guards guardsContext2, string parameter, string parameterName, Action<StringGuards> validate)
    {
        StringGuards stringGuards = new StringGuards(parameter, parameterName, guardsContext2.Errors);
        validate(stringGuards);

        return guardsContext2;
    }


    public static Guards Int(this Guards guardsContext2, int parameter, string parameterName, Action<IntGuards> validate)
    {
        IntGuards stringGuards = new IntGuards(parameter, parameterName, guardsContext2.Errors);
        validate(stringGuards);

        return guardsContext2;
    }

    public static Guards Decimal(this Guards guardsContext2, decimal parameter, string parameterName, Action<DecimalGuards> validate)
    {
        DecimalGuards stringGuards = new DecimalGuards(parameter, parameterName, guardsContext2.Errors);
        validate(stringGuards);

        return guardsContext2;
    }


    public static Guards Float(this Guards guardsContext2, float parameter, string parameterName, Action<FloatGuards> validate)
    {
        FloatGuards stringGuards = new FloatGuards(parameter, parameterName, guardsContext2.Errors);
        validate(stringGuards);

        return guardsContext2;
    }

    public static Guards Long(this Guards guardsContext2, long parameter, string parameterName, Action<LongGuards> validate)
    {
        LongGuards stringGuards = new LongGuards(parameter, parameterName, guardsContext2.Errors);
        validate(stringGuards);

        return guardsContext2;
    }

    public static Guards Double(this Guards guardsContext2, double parameter, string parameterName, Action<DoubleGuards> validate)
    {
        DoubleGuards stringGuards = new DoubleGuards(parameter, parameterName, guardsContext2.Errors);
        validate(stringGuards);

        return guardsContext2;
    }

    public static Guards Object(this Guards guardsContext2, object parameter, string parameterName, Action<ObjectGuards> validate)
    {
        ObjectGuards stringGuards = new ObjectGuards(parameter, parameterName, guardsContext2.Errors);
        validate(stringGuards);

        return guardsContext2;
    }
}