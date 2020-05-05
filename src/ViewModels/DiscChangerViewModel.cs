using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.IO;
using System.Reactive;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Reactive.Disposables;

namespace Synfonia.ViewModels
{
    public class DiscChangerViewModel : ViewModelBase
    {
        private ITrackList _trackList;
        private AudioEngine _audioEngine;
        private SoundStream _soundStream;
        private CompositeDisposable _soundStreamDisposables;
        private int _currentTrackIndex;
        private Track _currentTrack;
        private bool _isPlaying;
        private bool _userOperation = false;

        private bool _sliderClicked;
        private double _seekPosition;

        public DiscChangerViewModel()
        {
            _audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }

            PlayCommand = ReactiveCommand.CreateFromTask(async () =>
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

                    Play();
                }
            });

            ForwardCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Forward();
            });

            BackCommand = ReactiveCommand.CreateFromTask(async () =>
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

                Play();
            });
        }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }

        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public async Task LoadTrackList(ITrackList trackList)
        {
            _trackList = trackList;
            _currentTrackIndex = 0;

            await LoadTrack(_trackList.Tracks[_currentTrackIndex]);

            Play();
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

                MainWindowViewModel.Instance.TrackStatus.LoadTrack(track);

                _soundStream.WhenAnyValue(x => x.Position)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x =>
                    {
                        MainWindowViewModel.Instance.TrackStatus.UpdateCurrentPlayTime(x);
                    })
                    .DisposeWith(_soundStreamDisposables);

                Observable.FromEventPattern<double[]>(_soundStream, nameof(_soundStream.FFTDataReady))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x =>
                    {
                        MainWindowViewModel.Instance.TrackStatus.InFFTData = x.EventArgs;
                    })
                    .DisposeWith(_soundStreamDisposables);

                _soundStream.WhenAnyValue(x => x.State)
                    .ObserveOn(RxApp.MainThreadScheduler)
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



        public void Seek(TimeSpan seektime)
        {
            if (_isPlaying)
            {
                _soundStream.TrySeek(seektime);
            }
        }

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

            Play();

            _userOperation = false;
        }

        public void Play()
        {
            if (!_isPlaying && _soundStream != null)
            {
                _soundStream.PlayPause();

                _isPlaying = true;
            }
        }

        private async Task Pause()
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                _soundStream.PlayPause();

                await Task.Delay(100);
            }
        }

        public bool SliderClicked
        {
            get => _sliderClicked;
            set => this.RaiseAndSetIfChanged(ref _sliderClicked, value, nameof(SliderClicked));
        }
    }
}