using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Symphony.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private TrackStatusViewModel _trackStatus;
        private Dictionary<string, Album> _albumsDictionary;
        private ObservableCollection<Album> _albums;
        private Album _selectedAlbum;
        private AudioEngine _audioEngine;
        private SoundStream _soundStream;
        private bool _sliderClicked;
        private double _seekPosition;
        private Album _currentAlbum;
        private int _currentTrackIndex;

        public MainWindowViewModel()
        {
            _albumsDictionary = new Dictionary<string, Album>();
            TrackStatus = new TrackStatusViewModel();
            Albums = new ObservableCollection<Album>();

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
                        _currentTrackIndex--;
                    }
                    else
                    {
                        _currentTrackIndex = _currentAlbum.Tracks.Count - 1;
                    }

                    await DoPlay();
                }
            });

            ForwardCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_currentAlbum != null)
                {
                    if (_currentTrackIndex < _currentAlbum.Tracks.Count - 1)
                    {
                        _currentTrackIndex++;
                    }
                    else
                    {
                        _currentTrackIndex = 0;
                    }

                    await DoPlay();
                }
            });

            PlayCommand = ReactiveCommand.CreateFromTask(DoPlay);

            ScanMusicFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        }

        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }

        public ObservableCollection<Album> Albums
        {
            get { return _albums; }
            set { this.RaiseAndSetIfChanged(ref _albums, value); }
        }

        public Album SelectedAlbum
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

        static List<string> SupportedFileExtensions = new List<string>()
        {
            "3ga", "669", "a52", "aac", "ac3", "adt", "adts", "aif", "aifc", "aiff",
            "amb", "amr", "aob", "ape", "au", "awb", "caf", "dts", "dsf", "dff", "flac", "it", "kar",
            "m4a", "m4b", "m4p", "m5p", "mka", "mlp", "mod", "mpa", "mp1", "mp2", "mp3", "mpc", "mpga", "mus",
            "oga", "ogg", "oma", "opus", "qcp", "ra", "rmi", "s3m", "sid", "spx", "tak", "thd", "tta",
            "voc", "vqf", "w64", "wav", "wma", "wv", "xa", "xm"
        };

        private async Task ScanMusicFolder(string path)
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                await ScanMusicFolder(directory);
            }

            var files = Directory.EnumerateFiles(path, "*.*");

            foreach (var file in files)
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

                        if (tag.Album is null)
                        {
                            tag.Album = "Unknown Album";
                        }

                        if (!_albumsDictionary.ContainsKey(tag.Album))
                        {
                            var album = new Album();

                            album.Artist = tag.AlbumArtists.Concat(tag.Artists).FirstOrDefault();
                            album.Title = tag.Album;
                            album.Tracks.Add(new Track
                            {
                                Album = album,
                                Path = file,
                                Title = tag.Title
                            });

                            album.Cover = tag.LoadAlbumCover();

                            _albumsDictionary[tag.Album] = album;

                            Albums.Add(album);

                            if (SelectedAlbum is null)
                            {
                                SelectedAlbum = album;
                            }
                        }
                        else
                        {
                            _albumsDictionary[tag.Album].Tracks.Add(new Track
                            {
                                Album = _albumsDictionary[tag.Album],
                                Path = file,
                                Title = tag.Title
                            });
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private async Task DoPlay()
        {
            if (_soundStream != null && _soundStream.IsPlaying)
            {
                _soundStream?.Stop();

                await Task.Delay(50);

                _soundStream?.Dispose();

                await Task.Delay(100);
            }

            if (_currentAlbum != SelectedAlbum)
            {
                _currentAlbum = SelectedAlbum;
                _currentTrackIndex = 0;
            }

            var targetTrack = _currentAlbum.Tracks[_currentTrackIndex].Path;

            _soundStream = new SoundStream(File.OpenRead(targetTrack), _audioEngine);

            TrackStatus.LoadTrack(_soundStream, targetTrack);

            _soundStream.Play();
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
