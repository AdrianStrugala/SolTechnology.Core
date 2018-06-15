using System;
using Microsoft.AspNetCore.Authentication;

namespace Comparex.GMS.Inventory.InventoryRegistry.Provider.Authentication
{
    public class DreamAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string AuthenticationHeaderName = "Authorization";
        public const string AuthenticationScheme = "DreamAuthentication";
        public string ProviderAuthenticationKey { get; set; }
    }
}
