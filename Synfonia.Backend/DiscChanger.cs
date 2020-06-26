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


        private TrackContainer _currentTrack;
        private TrackContainer _preloadedTrack;

        private CompositeDisposable _trackDisposables;
        private CompositeDisposable _internalDisposables;

        public DiscChanger()
        {
            _trackList = new Playlist();

            var audioEngine = AudioEngine.CreateDefault();

            if (audioEngine == null) throw new Exception("Failed to create an audio backend!");

            var specProcessor = new SpectrumProcessor();

            _soundSink = new SoundSink(audioEngine, specProcessor);

            _internalDisposables = new CompositeDisposable();

            Observable.FromEventPattern<double[,]>(specProcessor, nameof(specProcessor.FftDataReady))
                .Subscribe(x =>
                {
                    CurrentSpectrumData = x.EventArgs;
                    SpectrumDataReady?.Invoke(this, EventArgs.Empty);
                })
                .DisposeWith(_internalDisposables);

            _internalState = (DiscChangerState.Idle);

        }

        public Track CurrentTrack => _currentTrack?.Track;

        public TimeSpan CurrentTrackPosition => _currentTrack?.SoundStream.Position ?? TimeSpan.Zero;

        public TimeSpan CurrentTrackDuration => _currentTrack?.SoundStream.Duration ?? TimeSpan.Zero;

        public double[,] CurrentSpectrumData { get; private set; }

        public double Volume
        {
            get => _currentTrack.SoundStream?.Volume ?? 0d;
            set
            {
                if (_currentTrack != null) _currentTrack.SoundStream.Volume = (float)value;
            }
        }

        public IObservable<bool> CanPlay ;
        public bool IsPlaying => _internalState == DiscChangerState.Playing;


        public event EventHandler TrackChanged;

        public event EventHandler SpectrumDataReady;

        public event EventHandler TrackPositionChanged;

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
                _currentTrack?.SoundStream.TrySeek(seektime);
        }

        private void PlayCore()
        {
            if (_internalState == DiscChangerState.Paused)
                _currentTrack?.SoundStream.PlayPause();
        }

        private void PauseCore()
        {
            if (_internalState == DiscChangerState.Playing)
                _currentTrack?.SoundStream.PlayPause();
        }

        private async void ForwardCore()
        {
            _currentTrackIndex = GetNextTrackIndex(TrackIndexDirection.Forward);
            var track = _trackList.Tracks[_currentTrackIndex];
            await LoadAndPlayTrack(track).ConfigureAwait(false);
            await PreloadReset(GetNextTrackIndex(TrackIndexDirection.Forward)).ConfigureAwait(false);
        }

        private async void BackCore()
        {
            _currentTrackIndex = GetNextTrackIndex(TrackIndexDirection.Backward);
            var track = _trackList.Tracks[_currentTrackIndex];
            await LoadAndPlayTrack(track).ConfigureAwait(false);
            await PreloadReset(GetNextTrackIndex(TrackIndexDirection.Backward)).ConfigureAwait(false);
        }

        public async Task AppendTrackList(ITrackList trackList)
        {
            _trackList.AddTracks(trackList);
            await PreloadReset(GetNextTrackIndex(TrackIndexDirection.Backward)).ConfigureAwait(false);
        }

        private async Task PreloadReset(int preloadIndex)
        {

        }
 
        private async Task LoadAndPlayTrack(Track track)
        {
            var targetPath = track.Path;

            if (File.Exists(targetPath))
            {
                _internalState = DiscChangerState.LoadingTrack;

                var soundStr = new SoundStream(File.OpenRead(targetPath), _soundSink);
                _currentTrack = new TrackContainer(track, soundStr);

                _trackDisposables?.Dispose();

                _trackDisposables = new CompositeDisposable();

                _trackDisposables.Add(_currentTrack);

                _currentTrack.SoundStream.PlayPause();

                _currentTrack.WhenAnyValue(x => x.SoundStream.IsPlaying)
                             .Subscribe(x => _internalState = x ? DiscChangerState.Playing : DiscChangerState.Paused)
                             .DisposeWith(_trackDisposables);

            }
        }

        public void Dispose()
        {
            _currentTrack?.Dispose();
            _preloadedTrack?.Dispose();
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