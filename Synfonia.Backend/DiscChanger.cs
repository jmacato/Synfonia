using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Synfonia.Backend
{
    public class DiscChanger : ReactiveObject
    {
        private AudioEngine _audioEngine;
        private Playlist _trackList;
        private int _currentTrackIndex;
        private bool _isPlaying => CurrentlyPlayingTrack.SoundStream.IsPlaying;
        private bool _userOperation = false;
        private double[,] _lastSpectrumData;
        private bool _isPaused = true;
        private bool _gaplessPlaybackEnabled = true;
        private int preloadIndex;
        private TrackContainer CurrentlyPlayingTrack, PreloadNextTrack;

        CompositeDisposable disp;
        private SoundSink _soundSink;

        public DiscChanger()
        {
            _trackList = new Playlist();
            var audioEngine = AudioEngine.CreateDefault();

            if (audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }

            _soundSink = new SoundSink(audioEngine);

            Observable.FromEventPattern<EventArgs>(this, nameof(TrackChanged))
                      .Select(x => this.CurrentlyPlayingTrack)
                      .Where(x => x != null)
                      .Select(x => x.SoundStream)
                      .Subscribe(OnTrackChanged);
        }

        private async void OnTrackChanged(SoundStream soundStr)
        {
            disp?.Dispose();

            disp = new CompositeDisposable();

            if (_gaplessPlaybackEnabled & PreloadNextTrack is null)
            {
                var nextIndex = GetNextTrackIndex(_currentTrackIndex, TrackIndexDirection.Forward);

                if (nextIndex == (int)TrackIndexDirection.Error)
                {
                    return;
                }

                preloadIndex = nextIndex;
                PreloadNextTrack = await LoadTrackAsync(_trackList.Tracks[preloadIndex]);
            }

            soundStr.WhenAnyValue(x => x.Position)
                .Subscribe(x =>
                {
                    TrackPositionChanged?.Invoke(this, EventArgs.Empty);
                })
                .DisposeWith(disp);

            Observable.FromEventPattern<double[,]>(soundStr, nameof(soundStr.FFTDataReady))
                .Subscribe(x =>
                {
                    _lastSpectrumData = x.EventArgs;
                    SpectrumDataReady?.Invoke(this, EventArgs.Empty);
                })
                .DisposeWith(disp);

            soundStr.WhenAnyValue(x => x.State)
                .DistinctUntilChanged()
                .Subscribe(async x =>
                {
                    if (!_userOperation && x == SoundStreamState.Stop)
                    {
                        if (PreloadNextTrack != null)
                        {
                            CurrentlyPlayingTrack?.Dispose();
                            CurrentlyPlayingTrack = PreloadNextTrack;
                            PreloadNextTrack = null;
                            
                            _currentTrackIndex = preloadIndex;

                            TrackChanged?.Invoke(this, EventArgs.Empty);
                            DoPlay();
                        }
                        else
                            await Forward(false);
                    }

                    IsPaused = x == SoundStreamState.Paused;
                })
                .DisposeWith(disp);
        }

        public event EventHandler TrackChanged;

        public event EventHandler SpectrumDataReady;

        public event EventHandler TrackPositionChanged;

        public Track CurrentTrack => CurrentlyPlayingTrack?.Track;

        public TimeSpan CurrentTrackPosition => CurrentlyPlayingTrack?.SoundStream.Position ?? TimeSpan.Zero;

        public TimeSpan CurrentTrackDuration => CurrentlyPlayingTrack?.SoundStream.Duration ?? TimeSpan.Zero;

        public double[,] CurrentSpectrumData => _lastSpectrumData;

        public double Volume
        {
            get => CurrentlyPlayingTrack.SoundStream?.Volume ?? 0d;
            set
            {
                if (CurrentlyPlayingTrack != null) CurrentlyPlayingTrack.SoundStream.Volume = (float)value;
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => this.RaiseAndSetIfChanged(ref _isPaused, value, nameof(IsPaused));
        }

        public bool GaplessPlaybackEnabled
        {
            get => _gaplessPlaybackEnabled;
            set => this.RaiseAndSetIfChanged(ref _gaplessPlaybackEnabled, value, nameof(GaplessPlaybackEnabled));
        }

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

            if (_trackList is null)
            {
                return;
            }

            var nextIndex = GetNextTrackIndex(_currentTrackIndex, TrackIndexDirection.Forward);

            if (nextIndex == (int)TrackIndexDirection.Error)
            {
                return;
            }

            if (_userOperation)
                PreloadNextTrack?.Dispose();

            _currentTrackIndex = nextIndex;

            ChangeTrackAndPlay(_currentTrackIndex);

            _userOperation = false;
        }

        public async Task Back(bool byUser = true)
        {
            _userOperation = byUser;

            if (_trackList is null)
            {
                return;
            }

            var nextIndex = GetNextTrackIndex(_currentTrackIndex, TrackIndexDirection.Backward);

            if (nextIndex == (int)TrackIndexDirection.Error)
            {
                return;
            }

            if (_userOperation)
                PreloadNextTrack?.Dispose();

            _currentTrackIndex = nextIndex;

            ChangeTrackAndPlay(_currentTrackIndex);

            _userOperation = false;
        }

        public void Seek(TimeSpan seektime)
        {
            if (_isPlaying)
            {
                CurrentlyPlayingTrack?.SoundStream.TrySeek(seektime);
            }
        }

        public async Task Play()
        {
            if (CurrentlyPlayingTrack is null && _trackList.Tracks.Count > 0)
                await Forward(false);
            else
                DoPlay();
        }

        private void DoPlay()
        {
            CurrentlyPlayingTrack.SoundStream.PlayPause();
        }

        public async Task AppendTrackList(ITrackList trackList)
        {
            bool isEmpty = _trackList.Tracks.Count == 0;

            _trackList.AddTracks(trackList);

            if (_isPaused || isEmpty)
            {
                if (isEmpty)
                {
                    _currentTrackIndex = 0;
                }

                ChangeTrackAndPlay(_currentTrackIndex);
            }
        }

        private async void ChangeTrackAndPlay(int index)
        {
            CurrentlyPlayingTrack?.Dispose();
            CurrentlyPlayingTrack = await LoadTrackAsync(_trackList.Tracks[index]);
            TrackChanged?.Invoke(this, EventArgs.Empty);
            DoPlay();
        }

        public async Task LoadTrackList(ITrackList trackList)
        {
            if (trackList.Tracks.Count > 0)
            {
                _trackList = new Playlist();
                _trackList.AddTracks(trackList);
                _currentTrackIndex = 0;

                ChangeTrackAndPlay(_currentTrackIndex);
            }
        }

        private async Task<TrackContainer> LoadTrackAsync(Track track)
        {
            var targetPath = track.Path;

            SoundStream soundStr;

            if (File.Exists(targetPath))
            {
                soundStr = new SoundStream(File.OpenRead(targetPath), _soundSink);
                return new TrackContainer(track, soundStr);
            }
            else
            {
                return null;
            }
        }
    }
}
