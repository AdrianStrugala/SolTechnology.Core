namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static void IntNotZero(int parameter, string parameterName)
        {
            if (parameter == 0)
            {
                throw new ArgumentException($"Int [{parameterName}] cannot be zero!");
            }
        }
    }
}