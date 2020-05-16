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
            }

            return (int)TrackIndexDirection.Error;
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

            if (GaplessPlaybackEnabled)
            {
                _ = Task.Factory.StartNew(async () =>
                  {
                      await Task.Delay(TimeSpan.FromMilliseconds(100));
                      var preloadNextIndex = GetNextTrackIndex(nextIndex, TrackIndexDirection.Forward);
                      UpNextTrack = await LoadTrackAsync(_trackList.Tracks[preloadNextIndex]);
                      UpNextTrack.SoundStream.PlayPause();
                      UpNextTrack.SoundStream.PlayPause();
                      UpNextTrack.SoundStream.TrySeek(TimeSpan.Zero);
                  });
            }

            TrackContainer NextToPlay;

            if (UpNextTrack is null)
            {
                NextToPlay = await LoadTrackAsync(_trackList.Tracks[nextIndex]);

                if (NextToPlay is null)
                {
                    await Forward(false);
                    return;
                }

            }
            else
            {
                NextToPlay = UpNextTrack;
                UpNextTrack = null;
            }

            _currentTrackIndex = nextIndex;
            CurrentlyPlayingTrack = NextToPlay;
            TrackChanged?.Invoke(this, EventArgs.Empty);
            DoPlay();
            _userOperation = false;
        }

        public async Task Back(bool byUser = true)
        {
            if (_trackList is null)
            {
                return;
            }

            // await LoadTrack(_trackList.Tracks[_currentTrackIndex]);

            // DoPlay();
        }

        public void Seek(TimeSpan seektime)
        {
            if (_isPlaying)
            {
                CurrentlyPlayingTrack.SoundStream.TrySeek(seektime);
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
                    // await LoadTrack(_trackList.Tracks[_currentTrackIndex]);
                }
            }
        }

        private void DoPlay()
        {
            CurrentlyPlayingTrack?.SoundStream.PlayPause();
        }

        public async Task Pause()
        {
            if (_isPlaying)
            {
                CurrentlyPlayingTrack.SoundStream.PlayPause();

                await Task.Delay(100);
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

                await Forward();

                DoPlay();
            }
        }

        public async Task LoadTrackList(ITrackList trackList)
        {
            if (trackList.Tracks.Count > 0)
            {
                _trackList = new Playlist();
                _trackList.AddTracks(trackList);
                _currentTrackIndex = 0;

                _currentTrackIndex = -1;
                Forward();

                // await LoadTrack(_trackList.Tracks[_currentTrackIndex]);
                // DoPlay();
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
