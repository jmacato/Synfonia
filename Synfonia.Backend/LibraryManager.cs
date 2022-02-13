using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Nito.AsyncEx;
using File = TagLib.File;

namespace Synfonia.Backend
{
    public class LibraryManager
    {
        private static readonly List<string> SupportedFileExtensions = new List<string>
        {
            "3ga", "669", "a52", "aac", "ac3", "adt", "adts", "aif", "aifc", "aiff",
            "amb", "amr", "aob", "ape", "au", "awb", "caf", "dts", "dsf", "dff", "it", "kar",
            "m4a", "m4b", "m4p", "m5p", "mka", "mlp", "mod", "mpa", "mp1", "mp2", "mp3", "mpc", "mpga", "mus",
            "oga", "ogg", "oma", "opus", "qcp", "ra", "rmi", "s3m", "sid", "spx", "tak", "thd", "tta",
            "voc", "vqf", "w64", "wav", "wma", "wv", "xa", "xm"
        };

        private readonly AsyncLock _dbLock;

        public LibraryManager()
        {
            Database = new LiteDatabase(Path.Combine(Path.GetDirectoryName(typeof(LibraryManager).Assembly.Location), "library.db"));
            _dbLock = new AsyncLock();
            Albums = new ObservableCollection<Album>();
            Artists = new ObservableCollection<Artist>();
        }

        public ObservableCollection<Album> Albums { get; }
        
        public ObservableCollection<Artist> Artists { get; }

        private LiteDatabase Database { get; }

        public event EventHandler<string> StatusChanged;

        private async Task<IDisposable> LockDatabaseAsync()
        {
            return await _dbLock.LockAsync();
        }

        public async Task LoadLibrary()
        {
            using (await LockDatabaseAsync())
            {
                var db = Database;

                var artistsCollection = db.GetCollection<Artist>(Artist.CollectionName);
                var albumsCollection = db.GetCollection<Album>(Album.CollectionName);
                var tracksCollection = db.GetCollection<Track>(Track.CollectionName);

                foreach (var artistEntry in artistsCollection.Include(x => x.Albums).FindAll().OrderBy(x => x.Name))
                {
                    Artists.Add(artistEntry);
                    
                    foreach (var albumId in artistEntry.Albums.Select(x => x.AlbumId))
                    {
                        var albumEntry = albumsCollection.Include(x => x.Tracks).FindById(albumId);

                        albumEntry.Artist = artistEntry;

                        foreach (var track in albumEntry.Tracks) track.Album = albumEntry;

                        Albums.Add(albumEntry);
                    }
                }
            }
        }

        public async Task ScanMusicFolder(string path)
        {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

            var albumDictionary = new Dictionary<int, Album>();

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
                        using (var tagFile = File.Create(file))
                        {
                            var tag = tagFile.Tag;

                            if (tag is null) continue;

                            var artistName = tag.AlbumArtists.Concat(tag.Artists).FirstOrDefault();

                            if (artistName is null) artistName = "Unknown Artist";

                            var albumName = tag.Album ?? "Unknown Album";

                            var trackName = tag.Title ?? "Unknown Track";

                            var trackNumber = tag.Track;

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

                            var existingAlbum = albumsCollection.FindOne(x =>
                                x.ArtistId == existingArtist.ArtistId && x.Title == tag.Album.Trim());

                            var albumAdded = false;

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

                                albumDictionary.Add(existingAlbum.AlbumId, existingAlbum);

                                Albums.Add(existingAlbum);
                            }
                            else
                            {
                                if (albumDictionary.ContainsKey(existingAlbum.AlbumId))
                                    existingAlbum = albumDictionary[existingAlbum.AlbumId];
                                else
                                    albumDictionary[existingAlbum.AlbumId] = existingAlbum;
                            }

                            var existingTrack = tracksCollection.FindOne(x => x.Path == file);

                            if (existingTrack is null)
                            {
                                existingTrack = new Track
                                {
                                    Path = new FileInfo(file).FullName,
                                    Title = trackName,
                                    Album = existingAlbum,
                                    TrackNumber = trackNumber
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