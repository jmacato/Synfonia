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
                else
                {
                    // recreate tag
                    var track = Tracks.FirstOrDefault();

                    if (track != null)
                    {
                        using var tagFile = TagLib.File.Create(track.Path);

                        var tag = tagFile.Tag;

                        var coverData = tag.Pictures.Where(x => x.Type == PictureType.FrontCover).Concat(tag.Pictures)
                            .FirstOrDefault()?.Data.Data;

                        if (coverData != null)
                        {
                            var new_coverHash = CryptoMethods.ComputeSha256Hash(coverData);
                            if (CoverHash != new_coverHash)
                            {
                                CoverHash = new_coverHash;
                                using var dbContext = new LibraryDbContext();
                                dbContext.Update(this);
                                dbContext.SaveChanges();
                            }

                            coverPath = Path.Combine(LibraryManager.AlbumPicStore, new_coverHash);

                            using var filex = System.IO.File.OpenWrite(coverPath);
                            filex.Write(coverData);

                            return coverData;
                        }
                    }
                }
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
                var coverData = await client.GetByteArrayAsync(url);

                if (coverData != null)
                    foreach (var track in Tracks)
                        using (var tagFile = TagLib.File.Create(track.Path))
                        {
                            tagFile.Tag.Pictures = new[]
                            {
                                new Picture(new ByteVector(coverData, coverData.Length))
                                {
                                    Type = PictureType.FrontCover,
                                    MimeType = "image/jpeg"
                                }
                            };

                            var new_coverHash = CryptoMethods.ComputeSha256Hash(coverData);

                            if (CoverHash != new_coverHash)
                            {
                                CoverHash = new_coverHash;
                                using var dbContext = new LibraryDbContext();
                                dbContext.Update(this);
                                dbContext.SaveChanges();
                            }

                            var coverPath = Path.Combine(LibraryManager.AlbumPicStore, new_coverHash);

                            using var filex = System.IO.File.OpenWrite(coverPath);
                            filex.Write(coverData);
                            tagFile.Save();
                        }
            }
        }
    }
}