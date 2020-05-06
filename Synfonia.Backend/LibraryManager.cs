using LiteDB;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synfonia.Backend
{
    public class LibraryManager
    {
        private LiteDatabase _db;
        private AsyncLock _dbLock;
        private ObservableCollection<Album> _albums;

        private static readonly List<string> SupportedFileExtensions = new List<string>()
        {
            "3ga", "669", "a52", "aac", "ac3", "adt", "adts", "aif", "aifc", "aiff",
            "amb", "amr", "aob", "ape", "au", "awb", "caf", "dts", "dsf", "dff", "flac", "it", "kar",
            "m4a", "m4b", "m4p", "m5p", "mka", "mlp", "mod", "mpa", "mp1", "mp2", "mp3", "mpc", "mpga", "mus",
            "oga", "ogg", "oma", "opus", "qcp", "ra", "rmi", "s3m", "sid", "spx", "tak", "thd", "tta",
            "voc", "vqf", "w64", "wav", "wma", "wv", "xa", "xm"
        };

        public LibraryManager()
        {
            _db = new LiteDatabase("library.db");
            _dbLock = new Nito.AsyncEx.AsyncLock();
            _albums = new ObservableCollection<Album>();
        }

        public event EventHandler<string> StatusChanged;

        public ObservableCollection<Album> Albums => _albums;

        private async Task<IDisposable> LockDatabaseAsync()
        {
            return await _dbLock.LockAsync();
        }

        private LiteDatabase Database => _db;

        public async Task LoadLibrary()
        {
            using (await LockDatabaseAsync())
            {
                var db = Database;

                var artistsCollection = db.GetCollection<Artist>(Artist.CollectionName);
                var albumsCollection = db.GetCollection<Album>(Album.CollectionName);
                var tracksCollection = db.GetCollection<Track>(Track.CollectionName);

                foreach (var artistEntry in artistsCollection.Include(x => x.Albums).FindAll())
                {
                    foreach (var albumId in artistEntry.Albums.Select(x => x.AlbumId))
                    {
                        var albumEntry = albumsCollection.Include(x => x.Tracks).FindById(albumId);

                        albumEntry.Artist = artistEntry;

                        foreach (var track in albumEntry.Tracks)
                        {
                            track.Album = albumEntry;
                        }

                        _albums.Add(albumEntry);
                    }
                }
            }
        }

        public async Task ScanMusicFolder(string path)
        {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

            using (var dbLock = await LockDatabaseAsync())
            {
                var db = Database;
                var artistsCollection = db.GetCollection<Artist>(Artist.CollectionName);
                var albumsCollection = db.GetCollection<Album>(Album.CollectionName);
                var tracksCollection = db.GetCollection<Track>(Track.CollectionName);

                foreach (var file in files.Select(x => new FileInfo(x).FullName))
                {
                    if (!SupportedFileExtensions.Any(x => $".{x}" == Path.GetExtension(file).ToLower()))
                        continue;

                    try
                    {
                        using (var tagFile = TagLib.File.Create(file))
                        {
                            var tag = tagFile.Tag;

                            if (tag is null)
                            {
                                continue;
                            }

                            var artistName = tag.AlbumArtists.Concat(tag.Artists).FirstOrDefault();

                            if (artistName is null)
                            {
                                artistName = "Unknown Artist";
                            }

                            var albumName = tag.Album ?? "Unknown Album";

                            var trackName = tag.Title ?? "Unknown Track";

                            // TODO other what to do if we dont know anything about the track, ignore?

                            StatusChanged?.Invoke(this, $"Processing: {artistName}, {albumName}, {trackName}");

                            var existingArtist = artistsCollection.FindOne(x => x.Name == artistName.Trim());

                            if (existingArtist is null)
                            {
                                existingArtist = new Artist
                                {
                                    Name = artistName
                                };

                                artistsCollection.Insert(existingArtist);
                            }

                            var existingAlbum = albumsCollection.FindOne(x => x.ArtistId == existingArtist.ArtistId && x.Title == tag.Album.Trim());

                            bool albumAdded = false;

                            if (existingAlbum is null)
                            {
                                albumAdded = true;

                                existingAlbum = new Album
                                {
                                    Title = albumName,
                                    ArtistId = existingArtist.ArtistId,
                                    Artist = existingArtist
                                };

                                albumsCollection.Insert(existingAlbum);

                                existingArtist.Albums.Add(existingAlbum);

                                artistsCollection.Update(existingArtist);

                                _albums.Add(existingAlbum);
                            }
                            else
                            {
                                existingAlbum.Artist = existingArtist;
                            }

                            var existingTrack = tracksCollection.FindOne(x => x.Path == file);

                            if (existingTrack is null)
                            {
                                existingTrack = new Track
                                {
                                    Path = new FileInfo(file).FullName,
                                    Title = trackName,
                                    Album = existingAlbum
                                };

                                tracksCollection.Insert(existingTrack);

                                existingAlbum.Tracks.Add(existingTrack);

                                albumsCollection.Update(existingAlbum);
                            }
                            else
                            {
                                existingTrack.Album = existingAlbum;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }
    }
}
