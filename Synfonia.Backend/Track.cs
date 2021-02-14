using System.IO;
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

        [BsonIgnore]
        public async Task<TrackContainer> LoadAsync(SoundSink soundSink)
        {
            var targetPath = Path;

            if (targetPath.StartsWith("onedrive:"))
            {
                await OneDriveSession.Instance.Login();
                
                var drive = await OneDriveSession.Instance.Api.GetDrive();
                var item = await OneDriveSession.Instance.Api.GetItemFromDriveById(targetPath.Replace("onedrive:", ""), drive.Id);
                var itemStream = await OneDriveSession.Instance.Api.DownloadItem(item);

                var soundStream = new SoundStream(itemStream, soundSink);
                return new TrackContainer(this, soundStream);
            }
            else
            {
                if (File.Exists(targetPath))
                {
                    var soundStr = new SoundStream(File.OpenRead(targetPath), soundSink);
                    return new TrackContainer(this, soundStr);
                }
            }

            return null;
        }
    }
}