using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KoenZomers.OneDrive.Api;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Synfonia.Backend
{
    public class OneDriveSession
    {
        private ExtendedOneDriveGraphApi _api;

        public static OneDriveSession Instance = new OneDriveSession();

        public OneDriveSession()
        {
            _api = new ExtendedOneDriveGraphApi("b47995e6-938b-403b-b4f2-de3e588c6120");
        }
        
        public OneDriveApi Api => _api;

        public bool LoggedIn => _api.AccessToken is { };

        public async Task Login()
        {
            if (!LoggedIn)
            {
                var location = @"C:\Temp";
                var storageProperties =
                    new StorageCreationPropertiesBuilder(".msalcache.bin3",
                            location, "b47995e6-938b-403b-b4f2-de3e588c6120")
                        .WithMacKeyChain("msal_service", "msal_account")
                        .Build();

                IPublicClientApplication app = PublicClientApplicationBuilder.Create("b47995e6-938b-403b-b4f2-de3e588c6120")
                    .WithRedirectUri("http://localhost:1234")
                    .Build();

                // This hooks up the cross-platform cache into MSAL
                var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
                cacheHelper.RegisterCache(app.UserTokenCache);

                // Create an authentication provider by passing in a client application and graph scopes.
                List<string> scopes = new List<string> {"user.read", "offline_access"};
                
                // See if any cached accounts exist.
                var accounts = await app.GetAccountsAsync();

                if (accounts.Any())
                {
                    var refreshLogin = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                        .WithForceRefresh(true).ExecuteAsync();
                    
                    _api.ExternalAuthorise(refreshLogin);

                    await _api.GetAccessToken();
                }
                else
                {
                    var loginResult = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
                
                    _api.ExternalAuthorise(loginResult);

                    await _api.GetAccessToken();
                }
            }
        }
    }
}