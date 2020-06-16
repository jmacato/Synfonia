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
        private readonly SoundSink _soundSink;
        private int _currentTrackIndex;
        private bool _isPaused = true;
        private Playlist _trackList;
        private bool _userOperation;
        private TrackContainer _currentTrack;
        private TrackContainer _preloadedTrack;
        private CompositeDisposable _disposables;
        private int preloadIndex;

        public DiscChanger()
        {
            _trackList = new Playlist();

            var audioEngine = AudioEngine.CreateDefault();

            if (audioEngine == null) throw new Exception("Failed to create an audio backend!");

            var specProcessor = new SpectrumProcessor();

            _soundSink = new SoundSink(audioEngine, specProcessor);

            Observable.FromEventPattern<EventArgs>(this, nameof(TrackChanged))
                .Select(x => _currentTrack)
                .Where(x => x != null)
                .Select(x => x.SoundStream)
                .Subscribe(OnTrackChanged);

            Observable.FromEventPattern<double[,]>(specProcessor, nameof(specProcessor.FftDataReady))
                .Subscribe(x =>
                {
                    CurrentSpectrumData = x.EventArgs;
                    SpectrumDataReady?.Invoke(this, EventArgs.Empty);
                });
        }

        private bool _isPlaying => _currentTrack.SoundStream.IsPlaying;

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

        public bool IsPaused
        {
            get => _isPaused;
            set => this.RaiseAndSetIfChanged(ref _isPaused, value, nameof(IsPaused));
        }

        private async void OnTrackChanged(SoundStream soundStr)
        {
            _disposables?.Dispose();

            _disposables = new CompositeDisposable();

            if (_preloadedTrack is null)
            {
                var nextIndex = GetNextTrackIndex(_currentTrackIndex, TrackIndexDirection.Forward);

                if (nextIndex == (int)TrackIndexDirection.Error)
                {
                    return;
                }

                preloadIndex = nextIndex;
                _preloadedTrack = await LoadTrackAsync(_trackList.Tracks[preloadIndex]);
            }

            soundStr.WhenAnyValue(x => x.Position)
                .Subscribe(x => { TrackPositionChanged?.Invoke(this, EventArgs.Empty); })
                .DisposeWith(_disposables);

            soundStr.WhenAnyValue(x => x.State)
                .DistinctUntilChanged()
                .Subscribe(async x =>
                {
                    if (!_userOperation & x == SoundStreamState.Stop)
                    {
                        if (_preloadedTrack != null)
                        {
                            _currentTrack?.Dispose();
                            _currentTrack = _preloadedTrack;
                            _preloadedTrack = null;
                            _currentTrackIndex = preloadIndex;
                            TrackChanged?.Invoke(this, EventArgs.Empty);
                            DoPlay(false);
                        }
                        else
                        {
                            await Forward(false);
                        }
                    }

                    IsPaused = x == SoundStreamState.Paused;
                })
                .DisposeWith(_disposables);
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

        public async Task Forward(bool byUser = true)
        {
            _userOperation = byUser;

            if (_trackList is null) return;

            var nextIndex = GetNextTrackIndex(_currentTrackIndex, TrackIndexDirection.Forward);

            if (nextIndex == (int)TrackIndexDirection.Error) return;

            _currentTrackIndex = nextIndex;

            ChangeTrackAndPlay(_currentTrackIndex);

            _userOperation = false;
        }

        public async Task Back(bool byUser = true)
        {
            _userOperation = byUser;

            if (_trackList is null) return;

            var nextIndex = GetNextTrackIndex(_currentTrackIndex, TrackIndexDirection.Backward);

            if (nextIndex == (int)TrackIndexDirection.Error) return;

            _currentTrackIndex = nextIndex;

            ChangeTrackAndPlay(_currentTrackIndex);

            _userOperation = false;
        }

        public void Seek(TimeSpan seektime)
        {
            if (_isPlaying) _currentTrack?.SoundStream.TrySeek(seektime);
        }

        public async Task Play()
        {
            if (_currentTrack is null && _trackList.Tracks.Count > 0)
                await Forward(false);
            else
                DoPlay(true);
        }

        private void DoPlay(bool userOp = true)
        {
            if (_currentTrack == null) return;

            if (!userOp)
            {
                if (_currentTrack.SoundStream.State == SoundStreamState.Paused)
                    _currentTrack.SoundStream.PlayPause();
            }
            else
            {
                _currentTrack.SoundStream.PlayPause();
            }
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
            _currentTrack?.Dispose();
            _currentTrack = await LoadTrackAsync(_trackList.Tracks[index]);
            TrackChanged?.Invoke(this, EventArgs.Empty);
            DoPlay(false);
        }

        public async Task LoadTrackList(ITrackList trackList)
        {
            if (trackList.Tracks.Count > 0)
            {
                await AppendTrackList(trackList);
            }
        }

        private async Task<TrackContainer> LoadTrackAsync(Track track)
        {
            var targetPath = track.Path;

            if (File.Exists(targetPath))
            {
                var soundStr = new SoundStream(File.OpenRead(targetPath), _soundSink);
                return new TrackContainer(track, soundStr);
            }

            return null;
        }

        public void Dispose()
        {
            if(!IsPaused)
            {
                _currentTrack.SoundStream.PlayPause();                
            }

            _currentTrack?.Dispose();
            _preloadedTrack?.Dispose();
            _disposables?.Dispose();

            try
            {
                _soundSink?.Dispose();
            }
            catch(Exception)
            {
            }
        }
    }
}