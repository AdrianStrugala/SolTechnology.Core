namespace SolTechnology.Core.Guards
{
    public class Guards
    {
        public Guards()
        {
            Errors = new List<string>();
        }

        public readonly List<string> Errors;


        public void ThrowOnErrors()
        {
            if (Errors.Any())
            {
                throw new GuardsException(Errors.Select(e => new ArgumentException(e)));
            }
        }

        public bool IsValid()
        {
            return Errors.Any();
        }

        public Guards String(string parameter, string parameterName, Action<StringGuards> validate)
        {
            StringGuards stringGuards = new StringGuards(parameter, parameterName, Errors);
            validate(stringGuards);

            return this;
        }


        public Guards Int(int parameter, string parameterName, Action<IntGuards> validate)
        {
            IntGuards stringGuards = new IntGuards(parameter, parameterName, Errors);
            validate(stringGuards);

            return this;
        }


        public Guards Decimal(decimal parameter, string parameterName, Action<DecimalGuards> validate)
        {
            DecimalGuards stringGuards = new DecimalGuards(parameter, parameterName, Errors);
            validate(stringGuards);

            return this;
        }


        public Guards Float(float parameter, string parameterName, Action<FloatGuards> validate)
        {
            FloatGuards stringGuards = new FloatGuards(parameter, parameterName, Errors);
            validate(stringGuards);

            return this;
        }

        public Guards Long(long parameter, string parameterName, Action<LongGuards> validate)
        {
            LongGuards stringGuards = new LongGuards(parameter, parameterName, Errors);
            validate(stringGuards);

            return this;
        }
        public Guards Double(double parameter, string parameterName, Action<DoubleGuards> validate)
        {
            DoubleGuards stringGuards = new DoubleGuards(parameter, parameterName, Errors);
            validate(stringGuards);

            return this;
        }

        public Guards Object(object parameter, string parameterName, Action<ObjectGuards> validate)
        {
            ObjectGuards stringGuards = new ObjectGuards(parameter, parameterName, Errors);
            validate(stringGuards);

            return this;
        }
    }
}