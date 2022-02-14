namespace SolTechnology.Core.Guards
{
    public partial class Guards
    {
        public static void StringNotNullNorEmpty(string parameter, string parameterName)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                throw new ArgumentException($"String [{parameterName}] cannot be null nor empty!");
            }
        }
    }
}