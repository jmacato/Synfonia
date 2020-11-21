using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using TagLib;

namespace Synfonia.Backend
{
    public class Album : ITrackList
    {
        public const string CollectionName = "albums";

        private string _title;

        public Album()
        {
            Tracks = new ObservableCollection<Track>();
        }

        public int AlbumId { get; set; }

        public int ArtistId { get; set; }

        public string Title
        {
            get => Regex.Unescape(_title);
            set => _title = value;
        }

        [BsonIgnore]
        public Artist Artist { get; set; }

        [BsonRef(Track.CollectionName)]
        public ObservableCollection<Track> Tracks { get; set; }

        IList<Track> ITrackList.Tracks => Tracks;

        public byte[] LoadCoverArt()
        {
            var track = Tracks.FirstOrDefault();

            if (track != null)
            {
                if(!System.IO.File.Exists(track.Path)) return null;
                
                using var tagFile = File.Create(track.Path);

                var tag = tagFile.Tag;

                var cover = tag.Pictures.Where(x => x.Type == PictureType.FrontCover).Concat(tag.Pictures)
                    .FirstOrDefault();

                if (cover != null) return cover.Data.Data;
            }

            return null;
        }

        public async Task UpdateCoverArtAsync(string url)
        {
            var clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = delegate { return true; }
            };

            using (var client = new HttpClient(clientHandler))
            {
                var data = await client.GetByteArrayAsync(url);

                if (data != null)
                    foreach (var track in Tracks)
                        using (var tagFile = File.Create(track.Path))
                        {
                            tagFile.Tag.Pictures = new[]
                            {
                                new Picture(new ByteVector(data, data.Length))
                                {
                                    Type = PictureType.FrontCover,
                                    MimeType = "image/jpeg"
                                }
                            };

                            tagFile.Save();
                        }
            }
        }
    }
}