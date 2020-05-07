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
    public class DiscChanger
    {
        private AudioEngine _audioEngine;
        private SoundStream _soundStream;
        private CompositeDisposable _soundStreamDisposables;
        private ITrackList _trackList;
        private int _currentTrackIndex;
        private Track _currentTrack;
        private bool _isPlaying;
        private bool _userOperation = false;
        private double[,] _lastSpectrumData;

        public DiscChanger()
        {
            _audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }
        }

        public event EventHandler TrackChanged;

        public event EventHandler SpectrumDataReady;

        public event EventHandler TrackPositionChanged;

        public Track CurrentTrack => _currentTrack;

        public TimeSpan CurrentTrackPosition => _soundStream.Position;

        public TimeSpan CurrentTrackDuration => _soundStream.Duration;

        public double[,] CurrentSpectrumData => _lastSpectrumData;

        public async Task Forward(bool byUser = true)
        {
            _userOperation = byUser;

            if (_trackList is null)
            {
                return;
            }

            if (_currentTrackIndex < _trackList.Tracks.Count - 1)
            {
                _currentTrackIndex++;
            }
            else
            {
                _currentTrackIndex = 0;
            }

            await LoadTrack(_trackList.Tracks[_currentTrackIndex]);

            DoPlay();

            _userOperation = false;
        }

        public async Task Back(bool byUser = true)
        {
            if (_trackList is null)
            {
                return;
            }

            if (_currentTrackIndex > 0)
            {
                _currentTrackIndex--;
            }
            else
            {
                _currentTrackIndex = _trackList.Tracks.Count - 1;
            }

            await LoadTrack(_trackList.Tracks[_currentTrackIndex]);

            DoPlay();
        }

        public void Seek(TimeSpan seektime)
        {
            if (_isPlaying)
            {
                _soundStream.TrySeek(seektime);
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

                if (_currentTrack is null)
                {
                    await LoadTrack(_trackList.Tracks[_currentTrackIndex]);
                }

                DoPlay();
            }
        }

        private void DoPlay()
        {
            if (!_isPlaying && _soundStream != null)
            {
                _soundStream.PlayPause();

                _isPlaying = true;
            }
        }

        public async Task Pause()
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                _soundStream.PlayPause();

                await Task.Delay(100);
            }
        }

        public async Task LoadTrackList(ITrackList trackList)
        {
            if (trackList.Tracks.Count > 0)
            {
                _trackList = trackList;
                _currentTrackIndex = 0;

                await LoadTrack(_trackList.Tracks[_currentTrackIndex]);

                DoPlay();
            }
        }

        private async Task LoadTrack(Track track)
        {
            await Pause();

            _soundStream?.Stop();

            _currentTrack = track;
            var targetTrack = track.Path;

            if (File.Exists(targetTrack))
            {
                await Task.Delay(100);
                _soundStream?.Dispose();
                _soundStreamDisposables?.Dispose();

                _soundStreamDisposables = new CompositeDisposable();

                _soundStream = new SoundStream(File.OpenRead(targetTrack), _audioEngine);

                TrackChanged?.Invoke(this, EventArgs.Empty);

                _soundStream.WhenAnyValue(x => x.Position)
                    .Subscribe(x =>
                    {
                        TrackPositionChanged?.Invoke(this, EventArgs.Empty);
                    })
                    .DisposeWith(_soundStreamDisposables);

                Observable.FromEventPattern<double[,]>(_soundStream, nameof(_soundStream.FFTDataReady))
                    .Subscribe(x =>
                    {
                        _lastSpectrumData = x.EventArgs;
                        SpectrumDataReady?.Invoke(this, EventArgs.Empty);
                    })
                    .DisposeWith(_soundStreamDisposables);

                _soundStream.WhenAnyValue(x => x.State)
                    .Subscribe(async x =>
                    {
                        if (!_userOperation && _isPlaying && x == SoundStreamState.Stopped)
                        {
                            await Forward();
                        }
                    })
                    .DisposeWith(_soundStreamDisposables);
            }
        }
    }
}
