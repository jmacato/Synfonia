using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.IO;
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
        private Track _currentTrack;
        private bool _isPlaying => CurrentlyPlayingTrack?.SoundStream.IsPlaying ?? false;
        private bool _userOperation = false;
        private double[,] _lastSpectrumData;
        private bool _isPaused = true;
        private bool _gaplessPlaybackEnabled = true;
        TrackContainer CurrentlyPlayingTrack, UpNextTrack;

        public DiscChanger()
        {
            _trackList = new Playlist();
            _audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }
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
            if (_isPlaying)
            {
                await Pause();
            }
            else
            {
                if (_trackList is null)
                {
                    return;
                }

                if (_currentTrack is null && _trackList.Tracks.Count > 0)
                {
                    await Forward();
                }
            }
        }

        private void DoPlay()
        {
            if (!_isPlaying)
            {
                CurrentlyPlayingTrack.SoundStream.PlayPause();
            }
        }

        public async Task Pause()
        {
            if (_isPlaying)
            {
                CurrentlyPlayingTrack.SoundStream.PlayPause();
            }
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
            CompositeDisposable disp;

            if (File.Exists(targetPath))
            {
                disp = new CompositeDisposable();

                soundStr = new SoundStream(File.OpenRead(targetPath), _audioEngine);

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
                    .Subscribe(async x =>
                    {
                        if (!_userOperation && _isPlaying && x == SoundStreamState.Stopped)
                        {
                            await Forward();
                        }

                        IsPaused = x == SoundStreamState.Paused;
                    })
                    .DisposeWith(disp);

                disp.Add(soundStr);

                return new TrackContainer(track, soundStr, disp);
            }
            else
            {
                return null;
            }
        }
    }
}
