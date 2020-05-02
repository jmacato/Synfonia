using Avalonia.Media.Imaging;
using Id3;
using ReactiveUI;
using SharpAudio.Codec;
using System;
using System.IO;
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
            using (var file = new Mp3(path))
            {
                var tag = file.GetTag(Id3TagFamily.Version2X);

                var cover = tag.Pictures.FirstOrDefault(x => x.PictureType == Id3.Frames.PictureType.FrontCover);

                if (cover != null)
                {
                    using (var ms = new MemoryStream(cover.PictureData))
                    {
                        AlbumCover = new Bitmap(ms);
                    }
                }
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
