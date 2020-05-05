using LiteDB;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Synfonia.ViewModels
{
    public class CollectionExplorerViewModel : ViewModelBase
    {
        private LiteDatabase _db;
        private Nito.AsyncEx.AsyncLock _dbLock;

        private static readonly List<string> SupportedFileExtensions = new List<string>()
        {
            "3ga", "669", "a52", "aac", "ac3", "adt", "adts", "aif", "aifc", "aiff",
            "amb", "amr", "aob", "ape", "au", "awb", "caf", "dts", "dsf", "dff", "flac", "it", "kar",
            "m4a", "m4b", "m4p", "m5p", "mka", "mlp", "mod", "mpa", "mp1", "mp2", "mp3", "mpc", "mpga", "mus",
            "oga", "ogg", "oma", "opus", "qcp", "ra", "rmi", "s3m", "sid", "spx", "tak", "thd", "tta",
            "voc", "vqf", "w64", "wav", "wma", "wv", "xa", "xm"
        };

        private ObservableCollection<AlbumViewModel> _albums;
        private AlbumViewModel _selectedAlbum;
        private SelectArtworkViewModel _selectArtwork;

        public CollectionExplorerViewModel()
        {
            _db = new LiteDatabase("library.db");
            _dbLock = new Nito.AsyncEx.AsyncLock();

            Albums = new ObservableCollection<AlbumViewModel>();
            SelectArtwork = new SelectArtworkViewModel();

            ScanLibraryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.Run(async () => await ScanMusicFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TestMusic"),
                    (album, artist) =>
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            LoadAlbum(album, artist);
                        });
                    }));
            });
        }

        public SelectArtworkViewModel SelectArtwork
        {
            get { return _selectArtwork; }
            set { this.RaiseAndSetIfChanged(ref _selectArtwork, value); }
        }

        public ObservableCollection<AlbumViewModel> Albums
        {
            get { return _albums; }
            set { this.RaiseAndSetIfChanged(ref _albums, value); }
        }

        public AlbumViewModel SelectedAlbum
        {
            get { return _selectedAlbum; }
            set { this.RaiseAndSetIfChanged(ref _selectedAlbum, value); }
        }

        public LiteDatabase Database => _db;

        public ReactiveCommand<Unit, Unit> ScanLibraryCommand { get; }

        public async Task<IDisposable> LockDatabaseAsync()
        {
            return await _dbLock.LockAsync();
        }

        private void LoadAlbum(Album albumEntry, string artistName)
        {
            var album = new AlbumViewModel(albumEntry.AlbumId);

            album.Artist = artistName;
            album.Title = albumEntry.Title;

            album.Tracks = new List<TrackViewModel>();

            album.Tracks.AddRange(albumEntry.Tracks.Select(x => new TrackViewModel
            {
                Album = album,
                Path = x.Path,
                Title = x.Title
            }));

            album.Cover = albumEntry.LoadAlbumCover();

            Albums.Add(album);

            if (SelectedAlbum is null)
            {
                SelectedAlbum = album;
            }
        }

        internal async Task LoadLibrary()
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

                        LoadAlbum(albumEntry, artistEntry.Name ?? "Unknown Artist");
                    }
                }
            }
        }

        private async Task ScanMusicFolder(string path, Action<Album, string> onAlbumAdded)
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

                            RxApp.MainThreadScheduler.Schedule(() =>
                            {
                                MainWindowViewModel.Instance.TrackStatus.Status = $"Processing: {artistName}, {albumName}, {trackName}";
                            });

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
                                    ArtistId = existingArtist.ArtistId
                                };

                                albumsCollection.Insert(existingAlbum);

                                existingArtist.Albums.Add(existingAlbum);

                                artistsCollection.Update(existingArtist);
                            }

                            var existingTrack = tracksCollection.FindOne(x => x.Path == file);

                            if (existingTrack is null)
                            {
                                existingTrack = new Track
                                {
                                    Path = new FileInfo(file).FullName,
                                    Title = trackName
                                };

                                tracksCollection.Insert(existingTrack);

                                existingAlbum.Tracks.Add(existingTrack);

                                albumsCollection.Update(existingAlbum);

                                if (albumAdded)
                                {
                                    onAlbumAdded(existingAlbum, existingArtist.Name);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
