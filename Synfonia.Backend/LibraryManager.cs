using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Synfonia.Backend
{
    public class LibraryManager
    {
        public static string AlbumPicStore = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".synfonia/AlbumPictureStore");

        private static readonly List<string> SupportedFileExtensions = new List<string>
        {
            "3ga", "669", "a52", "aac", "ac3", "adt", "adts", "aif", "aifc", "aiff",
            "amb", "amr", "aob", "ape", "au", "awb", "caf", "dts", "dsf", "dff", "flac", "it", "kar",
            "m4a", "m4b", "m4p", "m5p", "mka", "mlp", "mod", "mpa", "mp1", "mp2", "mp3", "mpc", "mpga", "mus",
            "oga", "ogg", "oma", "opus", "qcp", "ra", "rmi", "s3m", "sid", "spx", "tak", "thd", "tta",
            "voc", "vqf", "w64", "wav", "wma", "wv", "xa", "xm"
        };

        public LibraryDbContext DatabaseContext { get; private set; }

        private AsyncLock _dbLock;

        // private readonly AsyncLock _dbLock;

        public LibraryManager()
        {
            // Database = new LiteDatabase("");
            DatabaseContext = new LibraryDbContext();

            if (!Directory.Exists(AlbumPicStore))
            {
                Directory.CreateDirectory(AlbumPicStore);
            }

            _dbLock = new AsyncLock();
            Albums = new ObservableCollection<Album>();
        }

        public ObservableCollection<Album> Albums { get; }

        public event EventHandler<string> StatusChanged;

        public async Task LoadLibrary()
        {
            using var dbLock = await LockDatabaseAsync();

            // Hack for disabling lazy loading. 
            foreach (var album in DatabaseContext.Artists)
            { }

            // Hack for disabling lazy loading.
            foreach (var album in DatabaseContext.Tracks)
            { }

            foreach (var album in DatabaseContext.Albums)
            {
                album.Tracks = new ObservableCollection<Track>(album.Tracks.OrderBy(x => x.TrackNumber).ToList());
                Albums.Add(album);
            }
        }

        private async Task<IDisposable> LockDatabaseAsync()
        {
            return await _dbLock.LockAsync();
        }

        public async Task ScanMusicFolder(string path)
        {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

            Parallel.ForEach(files.Select(x => new FileInfo(x).FullName), async (file) =>
            {
                if (!SupportedFileExtensions.Any(x => $".{x}" == Path.GetExtension(file).ToLower()))
                    return;

                try
                {
                    using var tagFile = TagLib.File.Create(file);

                    var tag = tagFile.Tag;

                    if (tag is null) return;

                    var artistName = tag.AlbumArtists.Concat(tag.Performers).FirstOrDefault();

                    if (artistName is null) artistName = "Unknown Artist";

                    var albumName = tag.Album ?? "Unknown Album";

                    var trackName = tag.Title ?? "Unknown Track";

                    var trackNumber = tag.Track;

                    var cover = tag.Pictures.Where(x => x.Type == TagLib.PictureType.FrontCover).Concat(tag.Pictures)
                                      .FirstOrDefault();

                    var coverHash = "NONE";

                    var coverData = cover.Data.Data;

                    using var dbLock = await LockDatabaseAsync();

                    if (cover != null)
                    {
                        coverHash = CryptoMethods.ComputeSha256Hash(coverData);
                        var coverPath = Path.Combine(AlbumPicStore, coverHash);

                        if (!File.Exists(coverPath))
                        {
                            using var filex = File.OpenWrite(coverPath);
                            await filex.WriteAsync(coverData);
                        }
                    }

                    // TODO other what to do if we dont know anything about the track, ignore?

                    StatusChanged?.Invoke(this, $"Processing: {artistName}, {albumName}, {trackName}");

                    var existingArtist = DatabaseContext.Artists.FirstOrDefault(x => x.Name == artistName.Trim());

                    if (existingArtist is null)
                    {
                        existingArtist = new Artist
                        {
                            Name = artistName,
                        };

                        DatabaseContext.Artists.Add(existingArtist);
                        DatabaseContext.SaveChanges();
                    }

                    var existingAlbum = DatabaseContext.Albums.FirstOrDefault(x =>
                         x.Artist.Name == existingArtist.Name && x.Title == tag.Album.Trim());

                    if (existingAlbum is null)
                    {
                        existingAlbum = new Album
                        {
                            Title = albumName,
                            Artist = existingArtist,
                            CoverHash = coverHash
                        };

                        DatabaseContext.Albums.Add(existingAlbum);
                        DatabaseContext.SaveChanges();
                    }

                    existingArtist.Albums.Add(existingAlbum);
                    DatabaseContext.Artists.Update(existingArtist);
                    DatabaseContext.Albums.Update(existingAlbum);
                    DatabaseContext.SaveChanges();

                    var existingTrack = DatabaseContext.Tracks.FirstOrDefault(x => x.Path == file);

                    if (existingTrack is null)
                    {
                        existingTrack = new Track
                        {
                            Path = new FileInfo(file).FullName,
                            Title = trackName,
                            Album = existingAlbum,
                            TrackNumber = (int)trackNumber
                        };

                        DatabaseContext.Tracks.Add(existingTrack);
                        DatabaseContext.SaveChanges();
                    }

                    existingAlbum.Tracks.Add(existingTrack);
                    DatabaseContext.Albums.Update(existingAlbum);
                    DatabaseContext.SaveChanges();

                    if (!Albums.Any(x => x.Title == existingAlbum.Title))
                    {
                        Albums.Add(existingAlbum);
                    }
                }
                catch (Exception e)
                {
                }


            });
        }
        // }
    }
}