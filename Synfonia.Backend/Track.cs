using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using SharpAudio.Codec;

namespace Synfonia.Backend
{
    public class Track
    {
        public const string CollectionName = "tracks";
        private string _title;

        public int TrackId { get; set; }

        public uint TrackNumber { get; set; }

        public string Title
        {
            get => Regex.Unescape(_title);
            set => _title = value;
        }

        [BsonIgnore]
        public Album Album { get; set; }

        public string Path { get; set; }
        
        public string MimeType { get; set; }

        [BsonIgnore]
        public async Task<Stream> LoadAsync()
        {
            var targetPath = Path;

            if (targetPath.StartsWith("onedrive:"))
            {
                await OneDriveSession.Instance.Login();
                
                var drive = await OneDriveSession.Instance.Api.GetDrive();
                var item = await OneDriveSession.Instance.Api.GetItemFromDriveById(targetPath.Replace("onedrive:", ""), drive.Id);

                var itemStream = await OneDriveSession.Instance.Api.DownloadItem(item);

                return itemStream;
            }
            else
            {
                if (File.Exists(targetPath))
                {
                    return File.OpenRead(targetPath);
                }
            }

            return null;
        }
    }
}