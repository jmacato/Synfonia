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

        public string CurrentTime
        {
            get { return _currentTime; }
            set { this.RaiseAndSetIfChanged(ref _currentTime, value); }
        }

        private string FormatTimeSpan(TimeSpan x)
        {
            return $"{x.Hours:00}:{x.Minutes:00}:{x.Seconds:00}:{(x.Milliseconds / 100):0}";
        }

        public void LoadTrack(SoundStream track, string path)
        {
            using (var file = TagLib.File.Create(path))
            {
                AlbumCover = file.Tag.LoadAlbumCover();
            }

            AlbumCoverVisible = true;

            Artist = track.Metadata.Artists.FirstOrDefault();

            TrackTitle = track.Metadata.Title;

            Duration = track.Duration;

            track.WhenAnyValue(x => x.Position)
                .Subscribe(x =>
                {
                    CurrentTime = FormatTimeSpan(x);
                });

            track.WhenAnyValue(x => x.Position)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(x => Position = ((x.TotalSeconds) / Duration.TotalSeconds) * 100);
        }
    }
}
