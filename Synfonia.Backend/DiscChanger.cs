using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using SharpAudio.SpectrumAnalysis;

namespace Synfonia.Backend
{
    public class DiscChanger : ReactiveObject, IDisposable
    {
        enum DiscChangerState
        {
            Idle,
            LoadingTrack,
            Playing,
            CmdTriggered,
            CurrentTrackDone,
            Paused,
        }

        private DiscChangerState _internalState;

        private readonly SoundSink _soundSink;
        private int _currentTrackIndex;

        private Playlist _trackList;


        private TrackContainer _currentTrackContainer;
        private TrackContainer _preloadedTrackContainer;
        private int _preloadedTrackIndex;
        private CompositeDisposable _trackDisposables;
        private CompositeDisposable _internalDisposables;

        public DiscChanger()
        {
            _trackList = new Playlist();

            var audioEngine = AudioEngine.CreateDefault();

            if (audioEngine == null) throw new Exception("Failed to create an audio backend!");

            var spectrumProcessor = new SpectrumProcessor();

            _soundSink = new SoundSink(audioEngine, spectrumProcessor);

            _internalDisposables = new CompositeDisposable();

            Observable.FromEventPattern<double[,]>(spectrumProcessor, nameof(spectrumProcessor.FftDataReady))
                .Subscribe(x =>
                {
                    CurrentSpectrumData = x.EventArgs;
                })
                .DisposeWith(_internalDisposables);

            _internalState = (DiscChangerState.Idle);

        }

        private Track _currentTrack;
        private TimeSpan _currentTrackDuration;
        private TimeSpan _currentTrackPosition;
        private double[,] _currentSpectrumData;

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
                if (_currentTrackContainer != null) _currentTrackContainer.SoundStream.Volume = (float)value;
            }
        }

        public IObservable<bool> CanPlay;
        public bool IsPlaying => _internalState == DiscChangerState.Playing;


        public int GetNextTrackIndex(TrackIndexDirection direction)
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
                    return (int)TrackIndexDirection.Error;
            }
        }

        public void Seek(TimeSpan seektime) => CommandTriggerWrap(() => SeekCore(seektime));
        public void Play() => CommandTriggerWrap(PlayCore);
        public void Pause() => CommandTriggerWrap(PauseCore);
        public void Forward() => CommandTriggerWrap(ForwardCore);
        public void Back() => CommandTriggerWrap(BackCore);

        private volatile bool IsBusy = false;

        private void CommandTriggerWrap(Action CoreMethod)
        {
            if (!IsBusy)
            {
                IsBusy = true;
                CoreMethod();
                IsBusy = false;
            }
        }

        private void SeekCore(TimeSpan seektime)
        {
            if (_internalState == DiscChangerState.Playing)
                _currentTrackContainer?.SoundStream.TrySeek(seektime);
        }

        private void PlayCore()
        {
            if (_internalState == DiscChangerState.Paused)
                _currentTrackContainer?.SoundStream.PlayPause();
        }

        private void PauseCore()
        {
            if (_internalState == DiscChangerState.Playing)
                _currentTrackContainer?.SoundStream.PlayPause();
        }

        private async void ForwardCore()
        {
            _currentTrackIndex = GetNextTrackIndex(TrackIndexDirection.Forward);
            var track = _trackList.Tracks[_currentTrackIndex];
            _currentTrackContainer = await LoadTrack(track).ConfigureAwait(false);
            await DoFinishLoad().ConfigureAwait(false);
            await PreloadNext(GetNextTrackIndex(TrackIndexDirection.Forward)).ConfigureAwait(false);
        }

        private async void BackCore()
        {
            _currentTrackIndex = GetNextTrackIndex(TrackIndexDirection.Backward);
            var track = _trackList.Tracks[_currentTrackIndex];
            _currentTrackContainer = await LoadTrack(track).ConfigureAwait(false);
            await DoFinishLoad().ConfigureAwait(false);
            await PreloadNext(GetNextTrackIndex(TrackIndexDirection.Backward)).ConfigureAwait(false);
        }

        public async Task AppendTrackList(ITrackList trackList)
        {
            _trackList.AddTracks(trackList);
            await PreloadNext(GetNextTrackIndex(TrackIndexDirection.Backward)).ConfigureAwait(false);
        }

        private async Task PreloadNext(int preloadIndex)
        {
            _preloadedTrackContainer = await LoadTrack(_trackList.Tracks[preloadIndex]).ConfigureAwait(false);
            _preloadedTrackIndex = preloadIndex;
        }

        private async Task DoFinishLoad()
        {
            _trackDisposables?.Dispose();

            _trackDisposables = new CompositeDisposable();

            _trackDisposables.Add(_currentTrackContainer);

            _currentTrackContainer.SoundStream.WhenAnyValue(x => x.IsPlaying)
                         .Subscribe(x => _internalState = x ? DiscChangerState.Playing : DiscChangerState.Paused)
                         .DisposeWith(_trackDisposables);

            _currentTrackContainer.SoundStream.WhenAnyValue(x => x.Position)
                         .Subscribe(x => this.CurrentTrackPosition = x)
                         .DisposeWith(_trackDisposables);

            _currentTrackContainer.SoundStream.WhenAnyValue(x => x.Duration)
                         .Subscribe(x => this.CurrentTrackDuration = x)
                         .DisposeWith(_trackDisposables);

            _currentTrackContainer.SoundStream.WhenAnyValue(x => x.State)
                         .Where(x => x == SoundStreamState.Stop)
                         .Subscribe(CurrentTrackFinished)
                         .DisposeWith(_trackDisposables);

            CurrentTrack = _currentTrackContainer.Track;

            PlayCore();
        }

        private async void CurrentTrackFinished(SoundStreamState obj)
        {
            if (_preloadedTrackContainer is null)
            {
                ForwardCore();
            }
            else
            {
                _currentTrackContainer = _preloadedTrackContainer;
                _currentTrackIndex = _preloadedTrackIndex;
                await DoFinishLoad().ConfigureAwait(false);
            }
        }

        private async Task<TrackContainer> LoadTrack(Track track)
        {
            var targetPath = track.Path;

            if (File.Exists(targetPath))
            {
                _internalState = DiscChangerState.LoadingTrack;

                var soundStr = new SoundStream(File.OpenRead(targetPath), _soundSink);
                return new TrackContainer(track, soundStr);
            }

            return null;
        }

        public void Dispose()
        {
            _currentTrackContainer?.Dispose();
            _preloadedTrackContainer?.Dispose();
            _trackDisposables?.Dispose();

            try
            {
                _soundSink?.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}