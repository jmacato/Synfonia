using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using Synfonia.Backend.SpectrumAnalysis;

namespace Synfonia.Backend
{
    public class DiscChanger : ReactiveObject, IDisposable
    {
        private DiscChangerState _internalState;
        private Playlist _trackList;
        private TrackContainer _currentTrackContainer;
       // private TrackContainer _preloadedTrackContainer;
        private CompositeDisposable _trackDisposables;
        private CompositeDisposable _internalDisposables;
        private Track _currentTrack;
        private TimeSpan _currentTrackDuration;
        private TimeSpan _currentTrackPosition;
        private double[,] _currentSpectrumData;
        private volatile bool IsBusy = false;
        private int _currentTrackIndex;
       // private int _preloadedTrackIndex;
        private bool _IsPlaying;
        private readonly SpectrumProcessor _spectrumProcessor;
        private readonly AudioEngine _engine;
        

        public DiscChanger()
        {
            _trackList = new Playlist();

            _engine = AudioEngine.CreateDefault();

            if (_engine == null) throw new Exception("Failed to create an audio backend!");

            _spectrumProcessor = new SpectrumProcessor();

            _internalDisposables = new CompositeDisposable();

            Observable.FromEventPattern<double[,]>(_spectrumProcessor, nameof(_spectrumProcessor.FftDataReady))
                .Subscribe(x =>
                {
                    CurrentSpectrumData = x.EventArgs;
                })
                .DisposeWith(_internalDisposables);

            this.WhenAnyValue(x => x.InternalState)
                .DistinctUntilChanged()
                .Subscribe(x => IsPlaying = (x == DiscChangerState.Playing))
                .DisposeWith(_internalDisposables);

            InternalState = (DiscChangerState.Idle);
        }

        public async Task LoadTrackList(ITrackList trackList)
        {
            await ReplaceTrackList(trackList);

            _currentTrackIndex = -1;
            _currentTrackIndex = GetNextTrackIndex(TrackIndexDirection.Forward);
            var track = _trackList.Tracks[_currentTrackIndex];
            _currentTrackContainer = LoadTrack(track);
            await TrackContainerPlay(_currentTrackContainer);
        }

        private DiscChangerState InternalState
        {
            get => _internalState;
            set => this.RaiseAndSetIfChanged(ref _internalState, value, nameof(InternalState));
        }

        public ITrackList TrackList
        {
            get => _trackList;
        }

        public Track CurrentTrack
        {
            get => _currentTrack;
            private set => this.RaiseAndSetIfChanged(ref _currentTrack, value, nameof(CurrentTrack));
        }

        public TimeSpan CurrentTrackDuration
        {
            get => _currentTrackDuration;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackDuration, value, nameof(CurrentTrackDuration));
        }

        public TimeSpan CurrentTrackPosition
        {
            get => _currentTrackPosition;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackPosition, value, nameof(CurrentTrackPosition));
        }

        public double[,] CurrentSpectrumData
        {
            get => _currentSpectrumData;
            private set => this.RaiseAndSetIfChanged(ref _currentSpectrumData, value, nameof(CurrentSpectrumData));
        }

        public double Volume
        {
            get => _currentTrackContainer.SoundStream?.Volume ?? 0d;
            set
            {
                if (_currentTrackContainer != null) _currentTrackContainer.SoundStream.Volume = (float) value;
            }
        }

        public bool IsPlaying
        {
            get => _IsPlaying;
            private set => this.RaiseAndSetIfChanged(ref _IsPlaying, value, nameof(IsPlaying));
        }

        public void Seek(TimeSpan seektime) => CommandLock(() => SeekCore(seektime));
        public void Play() => CommandLock(PlayCore);
        public void Pause() => CommandLock(PauseCore);
        public void Forward() => CommandLock(() => NavigateCore(TrackIndexDirection.Forward));
        public void Back() => CommandLock(() => NavigateCore(TrackIndexDirection.Backward));

        public async Task AppendTrackList(ITrackList trackList)
        {
            var isLast = (_trackList.Tracks.Count - 1) == _currentTrackIndex;

            _trackList.AddTracks(trackList);

           // if (isLast)
               // await PreloadNext();
        }

        public async Task ReplaceTrackList(ITrackList trackList)
        {
            _trackList.Clear();
            
            _trackList.AddTracks(trackList);
            
            this.RaisePropertyChanged(nameof(TrackList));
        }

        private void CommandLock(Action CoreMethod)
        {
            if (!IsBusy)
            {
                IsBusy = true;
                Task.Factory.StartNew(() =>
                {
                    CoreMethod();
                    IsBusy = false;
                });
            }
        }

        private void SeekCore(TimeSpan seektime)
        {
            if (InternalState == DiscChangerState.Playing)
                _currentTrackContainer?.SoundStream.TrySeek(seektime);
        }

        private void PlayCore()
        {
            if (InternalState == DiscChangerState.Paused)
            {
                _currentTrackContainer?.SoundStream.Play();
                InternalState = DiscChangerState.Playing;
            }
        }

        private void PauseCore()
        {
            if (InternalState == DiscChangerState.Playing)
            {
                _currentTrackContainer?.SoundStream.Play();
                InternalState = DiscChangerState.Paused;
            }
        }

        private async void NavigateCore(TrackIndexDirection dir)
        {
            if (_trackList.Tracks.Count == 0) return;

            _currentTrackContainer?.SoundStream.Stop();
            _currentTrackContainer?.SoundStream.Dispose();
            _currentTrackContainer?.Dispose();
            //_preloadedTrackContainer?.Dispose();

            _currentTrackIndex = GetNextTrackIndex(dir);

            var track = _trackList.Tracks[_currentTrackIndex];

            _currentTrackContainer = LoadTrack(track);

            await TrackContainerPlay(_currentTrackContainer);
        }

        private int GetNextTrackIndex(TrackIndexDirection direction)
        {
            switch (direction)
            {
                case TrackIndexDirection.Forward:
                    if (_currentTrackIndex < _trackList.Tracks.Count - 1)
                    {
                        var r = _currentTrackIndex + 1;
                        return r;
                    }
                    else
                    {
                        return 0;
                    }

                case TrackIndexDirection.Backward:
                    if (_currentTrackIndex > 0)
                    {
                        var r = _currentTrackIndex - 1;
                        return r;
                    }
                    else
                    {
                        return _trackList.Tracks.Count - 1;
                    }
                default:
                    return (int) TrackIndexDirection.Error;
            }
        }

        private async Task PreloadNext()
        {
            await Task.Factory.StartNew(() =>
            {
               // _preloadedTrackIndex = GetNextTrackIndex(TrackIndexDirection.Forward);
              //  _preloadedTrackContainer = LoadTrack(_trackList.Tracks[_preloadedTrackIndex]);
            }).ConfigureAwait(false);
        }

        private async Task TrackContainerPlay(TrackContainer trackContainer)
        {
            _trackDisposables?.Dispose();
            _trackDisposables = new CompositeDisposable();
            _trackDisposables.Add(trackContainer);

            InternalState = DiscChangerState.Paused;

            var _trackSoundStrm = trackContainer.SoundStream;

            _trackSoundStrm.WhenAnyValue(x => x.State)
                .Subscribe(x =>
                    InternalState = x == SoundStreamState.Playing ? DiscChangerState.Playing : DiscChangerState.Paused)
                .DisposeWith(_trackDisposables);

            _trackSoundStrm.WhenAnyValue(x => x.Position)
                .Subscribe(x => this.CurrentTrackPosition = x)
                .DisposeWith(_trackDisposables);

            _trackSoundStrm.WhenAnyValue(x => x.Duration)
                .Subscribe(x => this.CurrentTrackDuration = x)
                .DisposeWith(_trackDisposables);

            _trackSoundStrm.WhenAnyValue(x => x.State)
                .DistinctUntilChanged()
                .Where(x => x == SoundStreamState.TrackFinished)
                .Take(1)
                .Subscribe(CurrentTrackFinished)
                .DisposeWith(_trackDisposables);

            _trackSoundStrm.WhenAnyValue(x => x.State)
                .DistinctUntilChanged()
                .Subscribe(x =>
                    InternalState = x == SoundStreamState.Playing ? DiscChangerState.Playing : DiscChangerState.Paused)
                .DisposeWith(_trackDisposables);

            CurrentTrack = trackContainer.Track;

            //await PreloadNext();

            PlayCore();
        }

        private async void CurrentTrackFinished(SoundStreamState obj)
        {
           // _currentTrackContainer = _preloadedTrackContainer;
           // _currentTrackIndex = _preloadedTrackIndex;
            await TrackContainerPlay(_currentTrackContainer);
        }

        private TrackContainer LoadTrack(Track track)
        {
            var targetPath = track.Path;

            if (File.Exists(targetPath))
            {
                var soundStr = new SoundStream(File.OpenRead(targetPath), new SoundSink(_engine, receiver: _spectrumProcessor));
                return new TrackContainer(track, soundStr);
            }

            return null;
        }

        public void Dispose()
        {
            try
            {
                _currentTrackContainer?.Dispose();
                _spectrumProcessor.Dispose();
                _trackDisposables?.Dispose();
                _internalDisposables?.Dispose();
                _engine.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}