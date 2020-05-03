using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace Symphony.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private CollectionExplorerViewModel _collectionExplorer;
        private TrackStatusViewModel _trackStatus;
        private AudioEngine _audioEngine;
        private SoundStream _soundStream;
        private bool _sliderClicked;
        private double _seekPosition;
        private AlbumViewModel _currentAlbum;
        private int _currentTrackIndex;

        public static MainWindowViewModel Instance { get; set; }

        public MainWindowViewModel()
        {
            TrackStatus = new TrackStatusViewModel();
            CollectionExplorer = new CollectionExplorerViewModel();

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
                        return;
                    }

                    if (_soundStream != null && _soundStream.IsPlaying)
                    {
                        _soundStream.Stop();
                        _soundStream = null;
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
                        return;
                    }


                    if (_soundStream != null && _soundStream.IsPlaying)
                    {
                        _soundStream.Stop();
                        _soundStream = null;
                    }


                    await DoPlay();
                }
            });

            PlayCommand = ReactiveCommand.CreateFromTask(DoPlay);

            CollectionExplorer.LoadLibrary();
        }



        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }

        public TrackStatusViewModel TrackStatus
        {
            get { return _trackStatus; }
            set { this.RaiseAndSetIfChanged(ref _trackStatus, value); }
        }

        public CollectionExplorerViewModel CollectionExplorer
        {
            get { return _collectionExplorer; }
            set { this.RaiseAndSetIfChanged(ref _collectionExplorer, value); }
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



        private async Task DoPlay()
        {
            if (_currentAlbum != CollectionExplorer.SelectedAlbum)
            {
                _currentAlbum = CollectionExplorer.SelectedAlbum;
                _currentTrackIndex = 0;

                _soundStream?.Stop();
            }
            else
            {
                _soundStream?.PlayPause();
                return;
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
