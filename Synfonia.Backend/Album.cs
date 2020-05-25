using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib;

namespace Synfonia.Backend
{
    public class Album : ITrackList
    {
        public int AlbumId { get; set; }

        public int ArtistId { get; set; }
        public Artist Artist { get; set; }
        public string Title { get; set; }
        public string CoverHash { get; set; }
        public ObservableCollection<Track> Tracks { get; set; } = new ObservableCollection<Track>();

        [NotMapped]
        IList<Track> ITrackList.Tracks => Tracks;

        public byte[] LoadCoverArt()
        {
            if (CoverHash != "NONE")
            {
                var coverPath = Path.Combine(LibraryManager.AlbumPicStore, CoverHash);
                if (System.IO.File.Exists(coverPath))
                {
                    return System.IO.File.ReadAllBytes(coverPath);
                }
            }

            return null;
        }

        public async Task UpdateCoverArtAsync(string url)
        {
            // var clientHandler = new HttpClientHandler
            // {
            //     ServerCertificateCustomValidationCallback = delegate { return true; }
            // };

            // using (var client = new HttpClient(clientHandler))
            // {
            //     var data = await client.GetByteArrayAsync(url);

            //     if (data != null)
            //         foreach (var track in Tracks)
            //             using (var tagFile = File.Create(track.Path))
            //             {
            //                 tagFile.Tag.Pictures = new[]
            //                 {
            //                     new Picture(new ByteVector(data, data.Length))
            //                     {
            //                         Type = PictureType.FrontCover,
            //                         MimeType = "image/jpeg"
            //                     }
            //                 };

            //                 tagFile.Save();
            //             }
            // }
        }
    }
}