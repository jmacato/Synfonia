using Avalonia.Threading;
using ReactiveUI;
using SharpAudio.Codec;
using System;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;

namespace Symphony.ViewModels
{
    public class TrackStatusViewModel : ViewModelBase
    {
        private string _artist;
        private string _trackTitle;
        private TimeSpan _duration;
        private double _position = 0.0d;
        private bool _albumCoverVisible;
        private string _currentTime;
        private object _albumCover;
        private double _seekPosition;
        private bool _isTrackSeeking;
        private bool _isSeekbarActive = true;
        private string _status;
        private double[] _fftData;

        public TrackStatusViewModel()
        {
            this.WhenAnyValue(x => x.SeekPosition)
                .Skip(1)
                .Subscribe(x =>
                {
                    MainWindowViewModel.Instance.DiscChanger.Seek(TimeSpan.FromSeconds(SeekPosition * Duration.TotalSeconds));
                });

            this.WhenAnyValue(x => x.Status)
                .Throttle(TimeSpan.FromSeconds(2))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    Status = "";
                });

        }

        public object AlbumCover
        {
            get { return _albumCover; }
            set { this.RaiseAndSetIfChanged(ref _albumCover, value); }
        }

        public bool AlbumCoverVisible
        {
            get { return _albumCoverVisible; }
            set { this.RaiseAndSetIfChanged(ref _albumCoverVisible, value); }
        }

        public string Artist
        {
            get { return _artist; }
            set { this.RaiseAndSetIfChanged(ref _artist, value); }
        }

        public string TrackTitle
        {
            get { return _trackTitle; }
            set { this.RaiseAndSetIfChanged(ref _trackTitle, value); }
        }

        public TimeSpan Duration
        {
            get => _duration;
            private set => this.RaiseAndSetIfChanged(ref _duration, value);
        }

        public double Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }
        public double SeekPosition
        {
            get => _seekPosition;
            set => this.RaiseAndSetIfChanged(ref _seekPosition, value);
        }

        public bool IsTrackSeeking
        {
            get => _isTrackSeeking;
            set => this.RaiseAndSetIfChanged(ref _isTrackSeeking, value);
        }

        public string CurrentTime
        {
            get { return _currentTime; }
            private set { this.RaiseAndSetIfChanged(ref _currentTime, value); }
        }

        public bool IsSeekbarActive
        {
            get => _isSeekbarActive;
            set => this.RaiseAndSetIfChanged(ref _isSeekbarActive, value);
        }

        public string Status
        {
            get { return _status; }
            set { this.RaiseAndSetIfChanged(ref _status, value); }
        }

        public double[] InFFTData
        {
            get { return _fftData; }
            set
            {
                _fftData = value;
                this.RaisePropertyChanged(nameof(InFFTData));
            }
        }

        private string FormatTimeSpan(TimeSpan x)
        {
            return $"{x.Hours:00}:{x.Minutes:00}:{x.Seconds:00}.{(x.Milliseconds / 100):0}";
        }

        public void UpdateCurrentPlayTime(TimeSpan time)
        {
            CurrentTime = FormatTimeSpan(time);

            if (!IsTrackSeeking)
            {
                Position = time.TotalSeconds / Duration.TotalSeconds;
            }
        }

        public void LoadTrack(Track track)
        {
            using (var file = TagLib.File.Create(track.Path))
            {
                SeekPosition = 0;

                AlbumCover = file.Tag.LoadAlbumCover();

                AlbumCoverVisible = true;

                Artist = file.Tag.AlbumArtists.Concat(file.Tag.Artists).FirstOrDefault();

                TrackTitle = file.Tag.Title;

                Duration = file.Properties.Duration;
            }
        }

    }
}
