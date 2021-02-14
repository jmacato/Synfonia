using System.Threading.Tasks;
using KoenZomers.OneDrive.Api;

namespace Synfonia.Backend
{
    public class OneDriveSession
    {
        private ExtendedOneDriveGraphApi _api;
        
        private const string refreshToken = "ADD A VALID REFRESH TOKEN SPECIFICALLY FOR YOUR ACCOUNT HERE.";

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
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _api.AuthenticateUsingRefreshToken(refreshToken);
                }
                else
                {
                    var authUri = _api.GetAuthenticationUri(); // get user to goto this address.

                    var token = _api.GetAuthorizationTokenFromUrl(""); // call this api with the return url.

                    await _api.GetAccessToken();
                }
            }
        }
    }
}