﻿using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Symphony.ViewModels
{
    public interface ITrackList
    {
        IList<Track> Tracks { get; }
    }

    public class DiscChangerViewModel : ViewModelBase
    {
        private ITrackList _trackList;
        private AudioEngine _audioEngine;
        private SoundStream _soundStream;
        private int _currentTrackIndex;
        private Track _currentTrack;
        private bool _isPlaying;

        private bool _sliderClicked;
        private double _seekPosition;

        public DiscChangerViewModel()
        {
            _audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }

            PlayPauseCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_isPlaying)
                {
                    await Pause();
                }
                else
                {
                    if (_currentTrack is null)
                    {
                        await LoadTrack(_trackList.Tracks[_currentTrackIndex]);
                    }

                    Play();
                }
            });

            ForwardCommand = ReactiveCommand.CreateFromTask(async () =>
            {
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
            });

            BackCommand = ReactiveCommand.CreateFromTask(async () =>
            {
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

        public ReactiveCommand<Unit, Unit> PlayPauseCommand { get; }

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

                _soundStream = new SoundStream(File.OpenRead(targetTrack), _audioEngine);

                MainWindowViewModel.Instance.TrackStatus.LoadTrack(track);

                _soundStream.WhenAnyValue(x => x.Position)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x =>
                    {
                        MainWindowViewModel.Instance.TrackStatus.UpdateCurrentPlayTime(x);
                    });
            }
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
                _soundStream.Pause();

                await Task.Delay(100);
            }
        }

        public bool SliderClicked
        {
            get => _sliderClicked;
            set => this.RaiseAndSetIfChanged(ref _sliderClicked, value, nameof(SliderClicked));
        }

        public double SeekPosition
        {
            get => _seekPosition;
            set { SliderChangedManually(value); }
        }





        public void SliderChangedManually(double value)
        {
            if (_soundStream is null) return;
            if (!_soundStream.IsPlaying) return;

            var x = ValidValuesOnly(value);
            //var z = TimeSpan.FromSeconds(x * Duration.TotalSeconds);
            //_soundStream.TrySeek(z);
        }

        private double ValidValuesOnly(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return 0;
            }
            else return Math.Clamp(value, 0d, 1000d);
        }
    }
}
