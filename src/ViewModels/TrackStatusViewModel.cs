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

        public void LoadTrack(SoundStream track)
        {
            AlbumCoverVisible = true;

            Artist = track.Metadata.Artists.FirstOrDefault();

            TrackTitle = track.Metadata.Title;

            Duration = track.Duration;

            track.WhenAnyValue(x => x.Position)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(x => Position = ((x.TotalSeconds / 250) / Duration.TotalSeconds) * 100);
        }
    }
}
