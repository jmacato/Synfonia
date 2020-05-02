using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            PlayCommand = ReactiveCommand.CreateFromTask(DoPlay);

            ScanMusicFolder(@"C:\Users\danwa\OneDrive\Music\Music\");
        }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

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

        private async Task ScanMusicFolder(string path)
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                await ScanMusicFolder(directory);
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.mp3"))
            {
                Console.WriteLine($"Processing file: {file}");

                using (var tagFile = TagLib.File.Create(file))
                {
                    var tag = tagFile.Tag;

                    if (tag is null)
                    {
                        continue;
                    }

                    if (!_albumsDictionary.ContainsKey(tag.Album))
                    {
                        var album = new Album();

                        album.Title = tag.Album;
                        album.Tracks.Add(new Track
                        {
                            Album = album,
                            Path = file,
                            Title = tag.Title
                        });

                        album.Cover = tag.LoadAlbumCover();

                        _albumsDictionary[tag.Album] = album;

                        if (album.Cover != null)
                        {
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

        private async Task DoPlay()
        {
            _soundStream?.Dispose();

            var targetTrack = SelectedAlbum.Tracks.FirstOrDefault().Path;

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
