using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class TrackStatusViewModel : ViewModelBase
    {
        private object _albumCover;
        private bool _albumCoverVisible;
        private string _artist;
        private string _currentTime;
        private string _currentDuration;
        private TimeSpan _duration;
        private double[,] _fftData;
        private bool _isSeekbarActive = true;
        private bool _isTrackSeeking;
        private double _position;
        private double _seekPosition;
        private string _status;
        private string _trackTitle;
        private string _albumTitle;

        public TrackStatusViewModel(DiscChanger discChanger, LibraryManager libraryManager)
        {
            this.WhenAnyValue(x => x.SeekPosition)
                .Skip(1)
                .Subscribe(x => { discChanger.Seek(TimeSpan.FromSeconds(SeekPosition * Duration.TotalSeconds)); });

            this.WhenAnyValue(x => x.Status)
                .Throttle(TimeSpan.FromSeconds(2))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { Status = ""; });

            Model = discChanger;

            Model.WhenAnyValue(x => x.CurrentTrack)
                 .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => LoadTrack(x));

            Model.WhenAnyValue(x => x.CurrentTrackPosition)
                 .ObserveOn(RxApp.MainThreadScheduler)
                 .Subscribe(x => UpdateCurrentPlayTime(x));

            Model.WhenAnyValue(x => x.CurrentSpectrumData)
                 .ObserveOn(RxApp.MainThreadScheduler)
                 .Subscribe(x => InFFTData = x);

            Observable.FromEventPattern<string>(libraryManager, nameof(libraryManager.StatusChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => Status = x.EventArgs);
        }

        public string FullTrack => Artist + " - " + AlbumTitle + " - "  + TrackTitle;

        public object AlbumCover
        {
            get => _albumCover;
            set => this.RaiseAndSetIfChanged(ref _albumCover, value);
        }

        public bool AlbumCoverVisible
        {
            get => _albumCoverVisible;
            set => this.RaiseAndSetIfChanged(ref _albumCoverVisible, value);
        }

        public string Artist
        {
            get => _artist;
            set => this.RaiseAndSetIfChanged(ref _artist, value);
        }

        public string TrackTitle
        {
            get => _trackTitle;
            set => this.RaiseAndSetIfChanged(ref _trackTitle, value);
        }

        public string AlbumTitle
        {
            get => _albumTitle;
            set => this.RaiseAndSetIfChanged(ref _albumTitle, value);
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
            get => _currentTime;
            private set => this.RaiseAndSetIfChanged(ref _currentTime, value);
        }

        public string CurrentDuration
        {
            get { return _currentDuration; }
            set { this.RaiseAndSetIfChanged(ref _currentDuration, value); }
        }


        public bool IsSeekbarActive
        {
            get => _isSeekbarActive;
            set => this.RaiseAndSetIfChanged(ref _isSeekbarActive, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public double[,] InFFTData
        {
            get => _fftData;
            set => this.RaiseAndSetIfChanged(ref _fftData, value);
        }

        public DiscChanger Model { get; }

        private string FormatTimeSpan(TimeSpan x)
        {
            return $"{x.Minutes:00}:{x.Seconds:00}.{x.Milliseconds / 100:0}";
        }

        private void UpdateCurrentPlayTime(TimeSpan time)
        {
            CurrentTime = FormatTimeSpan(time);

            if (!IsTrackSeeking) Position = time.TotalSeconds / Duration.TotalSeconds;
        }


        public async Task<Bitmap> LoadCoverAsync(Track track)
        {
            return await Task.Run(async () =>
            {
                var coverBitmap = track.Album.LoadCoverArt();

                if (coverBitmap != null)
                    using (var ms = new MemoryStream(coverBitmap))
                    {
                        return Bitmap.DecodeToWidth(ms, 200);
                    }

                return null;
            });
        }

        private void LoadTrack(Track track)
        {
            SeekPosition = 0;

            if (track is null) return;

            RxApp.MainThreadScheduler.Schedule(async () => { AlbumCover = await LoadCoverAsync(track); });

            AlbumCoverVisible = true;

            Artist = track.Album.Artist.Name;

            AlbumTitle = track.Album.Title;

            TrackTitle = track.Title;

            Duration = Model.CurrentTrackDuration;

            CurrentDuration = FormatTimeSpan(Duration);
            
            this.RaisePropertyChanged(nameof(FullTrack));
        }
    }
}