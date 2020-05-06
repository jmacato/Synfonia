using Avalonia;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SkiaSharp;
using Synfonia.Backend;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Synfonia.ViewModels
{
    public class TrackStatusViewModel : ViewModelBase
    {
        private DiscChanger _model;
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

        public TrackStatusViewModel(DiscChanger discChanger, LibraryManager libraryManager)
        {
            this.WhenAnyValue(x => x.SeekPosition)
                .Skip(1)
                .Subscribe(x =>
                {
                    discChanger.Seek(TimeSpan.FromSeconds(SeekPosition * Duration.TotalSeconds));
                });

            this.WhenAnyValue(x => x.Status)
                .Throttle(TimeSpan.FromSeconds(2))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    Status = "";
                });

            _model = discChanger;

            Observable.FromEventPattern(_model, nameof(_model.TrackChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => LoadTrack(_model.CurrentTrack));

            Observable.FromEventPattern(_model, nameof(_model.TrackPositionChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateCurrentPlayTime(_model.CurrentTrackPosition));

            Observable.FromEventPattern(_model, nameof(_model.SpectrumDataReady))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => InFFTData = _model.CurrentSpectrumData);

            Observable.FromEventPattern<string>(libraryManager, nameof(libraryManager.StatusChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => Status = x.EventArgs);
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

        private void UpdateCurrentPlayTime(TimeSpan time)
        {
            CurrentTime = FormatTimeSpan(time);

            if (!IsTrackSeeking)
            {
                Position = time.TotalSeconds / Duration.TotalSeconds;
            }
        }

        public unsafe Bitmap LoadBitmap(Stream stream)
        {
            var skBitmap = SKBitmap.Decode(stream);

            var scale = 200.0 / skBitmap.Width;

            var height = (int)(skBitmap.Height * scale);

            skBitmap = skBitmap.Resize(new SKImageInfo(200, height), SKFilterQuality.High);

            fixed (byte* p = skBitmap.Bytes)
            {
                IntPtr ptr = (IntPtr)p;

                return new Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, ptr, new PixelSize(skBitmap.Width, skBitmap.Height), new Vector(96, 96), skBitmap.RowBytes);
            }
        }


        public async Task<Bitmap> LoadCoverAsync(Track track)
        {
            return await Task.Run(async () =>
            {
                var coverBitmap = track.Album.LoadCoverArt();

                if (coverBitmap != null)
                {
                    using (var ms = new MemoryStream(coverBitmap))
                    {
                        return LoadBitmap(ms);
                    }
                }

                return null;
            });
        }

        private void LoadTrack(Track track)
        {
            SeekPosition = 0;

            RxApp.MainThreadScheduler.Schedule(async () =>
            {
                AlbumCover = await LoadCoverAsync(track);
            });

            AlbumCoverVisible = true;

            Artist = track.Album.Artist.Name;

            TrackTitle = track.Title;

            Duration = _model.CurrentTrackDuration;
        }

    }
}
