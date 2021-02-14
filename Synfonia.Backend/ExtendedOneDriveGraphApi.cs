using System.IO;
using System.Threading.Tasks;
using KoenZomers.OneDrive.Api;
using KoenZomers.OneDrive.Api.Entities;

namespace Synfonia.Backend
{
    public class ExtendedOneDriveGraphApi : OneDriveGraphApi
    {
        public ExtendedOneDriveGraphApi(string applicationId) : base(applicationId)
        {
        }

        protected override async  Task<Stream> DownloadItemInternal(OneDriveItem item, string completeUrl)
        {
            var client = this.CreateHttpClient((await this.GetAccessToken()).AccessToken);
            return new ReadSeekableStream(await client.GetStreamAsync(completeUrl), (int) item.Size);
        }
    }
}