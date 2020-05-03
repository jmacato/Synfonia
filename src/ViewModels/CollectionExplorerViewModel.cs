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

namespace Symphony.ViewModels
{
    public class CollectionExplorerViewModel : ViewModelBase
    {
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

        public CollectionExplorerViewModel()
        {
            Albums = new ObservableCollection<AlbumViewModel>();

            ScanLibraryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.Run(() => ScanMusicFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive\\Music\\Music"),
                    (album, artist) =>
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            LoadAlbum(album, artist);
                        });
                    }));

                MainWindowViewModel.Instance.TrackStatus.Status = "";
            });
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

        public ReactiveCommand<Unit, Unit> ScanLibraryCommand { get; }

        private void LoadAlbum(Album albumEntry, string artistName)
        {
            var album = new AlbumViewModel();

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

        internal void LoadLibrary()
        {
            using (var db = new LiteDatabase("library.db"))
            {
                var artistsCollection = db.GetCollection<Artist>(Artist.CollectionName);
                var albumsCollection = db.GetCollection<Album>(Album.CollectionName);
                var tracksCollection = db.GetCollection<Track>(Track.CollectionName);

                foreach (var artistEntry in artistsCollection.Include(x => x.Albums).FindAll())
                {
                    foreach (var albumId in artistEntry.Albums.Select(x => x.AlbumId))
                    {
                        var albumEntry = albumsCollection.Include(x => x.Tracks).FindById(albumId);

                        LoadAlbum(albumEntry, artistEntry.Name);
                    }
                }
            }
        }

        private void ScanMusicFolder(string path, Action<Album, string> onAlbumAdded)
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                ScanMusicFolder(directory, onAlbumAdded);
            }

            var files = Directory.EnumerateFiles(path, "*.*");

            using (var db = new LiteDatabase("library.db"))
            {
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

                            var artistName = tag.AlbumArtists.Concat(tag.Artists).FirstOrDefault() ?? "Unknown Artist";

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

                            var existingAlbum = albumsCollection.FindOne(x => x.Title == tag.Album.Trim());

                            bool albumAdded = false;

                            if (existingAlbum is null)
                            {
                                albumAdded = true;

                                existingAlbum = new Album
                                {
                                    Title = albumName,
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
