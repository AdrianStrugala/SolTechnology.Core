namespace SolTechnology.Core.Sql.Testing
{
    using System;
    using System.Collections.Generic;

    public static class ConnectionStringParser
    {
        public static string DatabaseKey = "Database";
        public static string UserKey = "User ID";
        public static string PasswordKey = "Password";
        public static string PortKey = "Port";

        public static Dictionary<string, string> Parse(string connectionString)
        {
            var result = new Dictionary<string, string>();

            // Split the connection string into key-value pairs
            var pairs = connectionString.Split(';');

            foreach (var pair in pairs)
            {
                if (!string.IsNullOrWhiteSpace(pair))
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim();
                        var value = keyValue[1].Trim();

                        // Check for specific keys and store the values accordingly
                        if (key.Equals(DatabaseKey, StringComparison.OrdinalIgnoreCase))
                        {
                            result[DatabaseKey] = value;
                        }
                        else if (key.Equals(UserKey, StringComparison.OrdinalIgnoreCase))
                        {
                            result[UserKey] = value;
                        }
                        else if (key.Equals(PasswordKey, StringComparison.OrdinalIgnoreCase))
                        {
                            result[PasswordKey] = value;
                        }
                        else if (key.Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract the port if present
                            var dataSourceParts = value.Split(',');
                            if (dataSourceParts.Length == 2)
                            {
                                result[PortKey] = dataSourceParts[1];
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
