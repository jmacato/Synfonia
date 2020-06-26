using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
            PlayCmdTriggered,
            LoadingTrack,
            Playing,
            NextTrackCmdTriggered,
            PrevCmdTriggered,
            StopCmdTriggered,
            CurrentTrackDone,
        }

        private DiscChangerState State;

        private readonly SoundSink _soundSink;
        private int _currentTrackIndex;

        private Playlist _trackList;


        private TrackContainer _currentTrack;
        private TrackContainer _preloadedTrack;

        private CompositeDisposable _trackDisposables;
        private CompositeDisposable _internalDisposables;

        public Task Pause()
        {
            throw new NotImplementedException();
        }

        public DiscChanger()
        {
            _trackList = new Playlist();

            var audioEngine = AudioEngine.CreateDefault();

            if (audioEngine == null) throw new Exception("Failed to create an audio backend!");

            var specProcessor = new SpectrumProcessor();

            _soundSink = new SoundSink(audioEngine, specProcessor);





            Observable.FromEventPattern<double[,]>(specProcessor, nameof(specProcessor.FftDataReady))
                .Subscribe(x =>
                {
                    CurrentSpectrumData = x.EventArgs;
                    SpectrumDataReady?.Invoke(this, EventArgs.Empty);
                }).DisposeWith(_internalDisposables);

        }

        private bool _isPlaying => _currentTrack?.SoundStream.IsPlaying ?? false;

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

        public IObservable<bool> CanPlay { get; }
        public bool IsPlaying { get; }

        private async void OnTrackChanged(SoundStream soundStr)
        {
            _trackDisposables?.Dispose();

            _trackDisposables = new CompositeDisposable();



        }

        public event EventHandler TrackChanged;

        public event EventHandler SpectrumDataReady;

        public event EventHandler TrackPositionChanged;

        public int GetNextTrackIndex(int index, TrackIndexDirection direction)
        {
            switch (direction)
            {
                case TrackIndexDirection.Forward:
                    if (index < _trackList.Tracks.Count - 1)
                    {
                        var r = index + 1;
                        return r;
                    }
                    else
                    {
                        return 0;
                    }

                case TrackIndexDirection.Backward:
                    if (index > 0)
                    {
                        var r = index - 1;
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

        public async Task Forward()
        {

        }

        public async Task Back()
        {

        }

        public void Seek(TimeSpan seektime)
        {
            if (_isPlaying) _currentTrack?.SoundStream.TrySeek(seektime);
        }

        public async Task Play()
        {

        }

        public async Task AppendTrackList(ITrackList trackList)
        {
            var isEmpty = _trackList.Tracks.Count == 0;

            _trackList.AddTracks(trackList);

            if (isEmpty)
            {
                _currentTrackIndex = -1;
                await Forward(false);
            }
        }

        private async void ChangeTrackAndPlay(int index)
        {

        }

        public async Task LoadTrackList(ITrackList trackList)
        {

        }

        private async Task<(TrackContainer, bool)> PlayTrackAsync(Track track)
        {
            var targetPath = track.Path;

            if (File.Exists(targetPath))
            {
                var soundStr = new SoundStream(File.OpenRead(targetPath), _soundSink);
                return (new TrackContainer(track, soundStr), true);
            }

            return (null, false);
        }

        public void Dispose()
        {
            if (!IsPaused)
            {
                _currentTrack.SoundStream.PlayPause();
            }

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