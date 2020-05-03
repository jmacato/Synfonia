using LiteDB;
using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Symphony.ViewModels
{
    public class Track
    {
        public const string CollectionName = "tracks";
        private string _path;
        private string _title;

        public int TrackId { get; set; }

        public string Title
        {
            get { return Regex.Unescape(_title); }
            set { _title = value; }
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
    }

    public class Album
    {
        public const string CollectionName = "albums";

        private string _title;

        public int AlbumId { get; set; }

        public string Title
        {
            get => Regex.Unescape(_title);
            set { _title = value; }
        }

        [BsonRef(Track.CollectionName)]
        public List<Track> Tracks { get; set; } = new List<Track>();
    }

    public class Artist
    {
        public const string CollectionName = "artists";
        private string _name;

        public int ArtistId { get; set; }

        public string Name
        {
            get { return Regex.Unescape(_name); }
            set { _name = value; }
        }


        [BsonRef(Album.CollectionName)]
        public List<Album> Albums { get; set; } = new List<Album>();
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private TrackStatusViewModel _trackStatus;
        private Dictionary<Album, AlbumViewModel> _albumsDictionary;
        private ObservableCollection<AlbumViewModel> _albums;
        private AlbumViewModel _selectedAlbum;
        private AudioEngine _audioEngine;
        private SoundStream _soundStream;
        private bool _sliderClicked;
        private double _seekPosition;
        private AlbumViewModel _currentAlbum;
        private int _currentTrackIndex;
        private SelectArtworkViewModel _selectArtwork;
        private bool _trackChanged;



        public static MainWindowViewModel Instance { get; set; }

        public MainWindowViewModel()
        {
            _albumsDictionary = new Dictionary<Album, AlbumViewModel>();
            TrackStatus = new TrackStatusViewModel();
            Albums = new ObservableCollection<AlbumViewModel>();
            SelectArtwork = new SelectArtworkViewModel();

            _audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }

            BackCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_currentAlbum != null)
                {
                    if (_currentTrackIndex > 0)
                    {
                        _trackChanged = true;
                        _currentTrackIndex--;
                    }
                    else
                    {
                        return;
                    }

                    if (_soundStream != null && _soundStream.IsPlaying)
                    {
                        _soundStream.Stop();
                        _soundStream = null;
                    }


                    await DoPlay();
                }
            }, this.WhenAnyValue(x => x.SelectedAlbum).Select(x => x?.Tracks.Count > 1));

            ForwardCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_currentAlbum != null)
                {
                    if (_currentTrackIndex < _currentAlbum.Tracks.Count - 1)
                    {
                        _trackChanged = true;
                        _currentTrackIndex++;
                    }
                    else
                    {
                        return;
                    }


                    if (_soundStream != null && _soundStream.IsPlaying)
                    {
                        _soundStream.Stop();
                        _soundStream = null;
                    }


                    await DoPlay();
                }
            }, this.WhenAnyValue(x => x.SelectedAlbum).Select(x => x?.Tracks.Count > 1));

            PlayCommand = ReactiveCommand.CreateFromTask(DoPlay);

            this.WhenAnyValue(x => x.SelectedAlbum)
                .Subscribe(x => _trackChanged = true);

            ScanMusicFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive\\Music\\Music"));

            LoadLibrary();
        }

        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }

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

        public TrackStatusViewModel TrackStatus
        {
            get { return _trackStatus; }
            set { this.RaiseAndSetIfChanged(ref _trackStatus, value); }
        }

        public bool SliderClicked
        {
            get => _sliderClicked;
            set => this.RaiseAndSetIfChanged(ref _sliderClicked, value, nameof(SliderClicked));
        }

        public double SeekPosition
        {
            get => _seekPosition;
            set { SliderChangedManually(value); }
        }

        private static readonly List<string> SupportedFileExtensions = new List<string>()
        {
            "3ga", "669", "a52", "aac", "ac3", "adt", "adts", "aif", "aifc", "aiff",
            "amb", "amr", "aob", "ape", "au", "awb", "caf", "dts", "dsf", "dff", "flac", "it", "kar",
            "m4a", "m4b", "m4p", "m5p", "mka", "mlp", "mod", "mpa", "mp1", "mp2", "mp3", "mpc", "mpga", "mus",
            "oga", "ogg", "oma", "opus", "qcp", "ra", "rmi", "s3m", "sid", "spx", "tak", "thd", "tta",
            "voc", "vqf", "w64", "wav", "wma", "wv", "xa", "xm"
        };

        public SelectArtworkViewModel SelectArtwork
        {
            get { return _selectArtwork; }
            set { this.RaiseAndSetIfChanged(ref _selectArtwork, value); }
        }

        private void LoadLibrary()
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

                        //if (!_albumsDictionary.ContainsKey(albumEntry))
                        {
                            var album = new AlbumViewModel();

                            album.Artist = artistEntry.Name;
                            album.Title = albumEntry.Title;

                            album.Tracks = new List<TrackViewModel>();

                            album.Tracks.AddRange(albumEntry.Tracks.Select(x => new TrackViewModel
                            {
                                Album = album,
                                Path = x.Path,
                                Title = x.Title
                            }));

                            album.Cover = albumEntry.LoadAlbumCover();

                            _albumsDictionary[albumEntry] = album;

                            Albums.Add(album);

                            if (SelectedAlbum is null)
                            {
                                SelectedAlbum = album;
                            }
                        }
                    }
                }
            }
        }

        private async Task ScanMusicFolder(string path)
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                await ScanMusicFolder(directory);
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

                    Debug.WriteLine($"Processing file: {file}");

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

                            if (existingAlbum is null)
                            {
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
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private async Task DoPlay()
        {

            if (_trackChanged)
            {
                _soundStream?.Stop();
                _trackChanged = false;
            }
            else
            {
                _soundStream?.PlayPause();
                return;
            }

            if (_currentAlbum != SelectedAlbum)
            {
                _currentAlbum = SelectedAlbum;
                _currentTrackIndex = 0;
            }

            var targetTrack = _currentAlbum.Tracks[_currentTrackIndex].Path;

            _soundStream = new SoundStream(File.OpenRead(targetTrack), _audioEngine);

            TrackStatus.LoadTrack(_soundStream, targetTrack);

            _soundStream.PlayPause();
        }

        public void SliderChangedManually(double value)
        {
            if (_soundStream is null) return;
            if (!_soundStream.IsPlaying) return;

            var x = ValidValuesOnly(value);
            //var z = TimeSpan.FromSeconds(x * Duration.TotalSeconds);
            //_soundStream.TrySeek(z);
        }

        private double ValidValuesOnly(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return 0;
            }
            else return Math.Clamp(value, 0d, 1000d);
        }
    }
}
