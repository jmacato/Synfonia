using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Espresso3389.HttpStream;
using KoenZomers.OneDrive.Api;
using KoenZomers.OneDrive.Api.Entities;
using Microsoft.Identity.Client;

namespace Synfonia.Backend
{
    public class ExtendedOneDriveGraphApi : OneDriveGraphApi
    {
        private OneDriveAccessToken _accessToken;
        
        public ExtendedOneDriveGraphApi(string applicationId) : base(applicationId)
        {
        }

        public void ExternalAuthorise(AuthenticationResult authenticationResult)
        {
            // a hack to make it work.
            GetAuthorizationTokenFromUrl("https://login.microsoftonline.com/common/oauth2/nativeclient?code=test"); // call this api with the return url.
            
            _accessToken = new OneDriveAccessToken
            {
                AccessToken = authenticationResult.AccessToken,
                AuthenticationToken = "test",
            };
        }

        protected override async Task<OneDriveAccessToken> GetAccessTokenFromAuthorizationToken(string authorizationToken)
        {
            if (_accessToken is { })
            {
                return _accessToken;
            }
            
            return await base.GetAccessTokenFromAuthorizationToken(authorizationToken);
        }

        protected override async  Task<Stream> DownloadItemInternal(OneDriveItem item, string completeUrl)
        {
            var client = this.CreateHttpClient((await this.GetAccessToken()).AccessToken);
            return new HttpStream(new Uri(completeUrl), new MemoryStream(), true, 32 * 1024, null, client);
            //return new PartialHttpStream(completeUrl, (await this.GetAccessToken()).AccessToken, (int)item.Size);
        }
    }
}