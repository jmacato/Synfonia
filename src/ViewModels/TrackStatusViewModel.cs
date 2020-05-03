using ReactiveUI;
using SharpAudio.Codec;
using System;
using System.Linq;
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
            set { this.RaiseAndSetIfChanged(ref _currentTime, value); }
        }

        private string FormatTimeSpan(TimeSpan x)
        {
            return $"{x.Hours:00}:{x.Minutes:00}:{x.Seconds:00}.{(x.Milliseconds / 100):0}";
        }

        public void LoadTrack(SoundStream track, string path)
        {
            using (var file = TagLib.File.Create(path))
            {
                AlbumCover = file.Tag.LoadAlbumCover();

                AlbumCoverVisible = true;

                Artist = file.Tag.AlbumArtists.Concat(file.Tag.Artists).FirstOrDefault();

                TrackTitle = file.Tag.Title;

                Duration = file.Properties.Duration;

                this.WhenAnyValue(x => x.SeekPosition)
                    .Subscribe(x =>
                    {
                        Console.WriteLine($"SeekPos {x}");
                        track.TrySeek(TimeSpan.FromSeconds(SeekPosition * Duration.TotalSeconds));
                    });

                track.WhenAnyValue(x => x.Position)
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Do(x => CurrentTime = FormatTimeSpan(x))
                            .Do(x => { if (!IsTrackSeeking) Position = x.TotalSeconds / Duration.TotalSeconds; })
                            .Subscribe();
            }
        }
    }
}
