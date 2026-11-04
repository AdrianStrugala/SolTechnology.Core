namespace SolTechnology.Core.Auth
{
    /// <summary>
    /// Configuration passed to <see cref="ModuleInstaller.AddSolAuth"/>.
    /// </summary>
    public class AuthConfiguration
    {
        /// <summary>The API key every request must send in the <c>X-API-KEY</c> header. Required.</summary>
        public string ApiKey { get; set; } = null!;
    }

}