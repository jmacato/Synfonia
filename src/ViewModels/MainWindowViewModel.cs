using System.Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using SharpAudio;
using SharpAudio.Codec;

namespace Symphony.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private TrackStatusViewModel _trackStatus;

        public MainWindowViewModel()
        {
            TrackStatus = new TrackStatusViewModel();

            this._audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }

            this.WhenAnyValue(x => x.TargetFile)
                 .DistinctUntilChanged()
                .Subscribe(x => this.TargetFile = x);

            TargetFile = @"C:\Users\danwa\OneDrive\Music\Music\Elton John\Greatest Hits (1970-2002) [3CD]\01 your song.mp3";

            PlayCommand = ReactiveCommand.CreateFromTask(DoPlay);
        }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public async Task DoPlay()
        {
            _soundStream = new SoundStream(File.OpenRead(TargetFile), _audioEngine);



            TrackStatus.LoadTrack(_soundStream);

            _soundStream.Play();
        }

        public void SliderChangedManually(double value)
        {
            if (_soundStream is null) return;
            if (!_soundStream.IsPlaying) return;

            var x = ValidValuesOnly(value);
            //var z = TimeSpan.FromSeconds(x * Duration.TotalSeconds);
            //_soundStream.TrySeek(z);
        }

        private string _targetFile;

        public string TargetFile
        {
            get => _targetFile;
            set => this.RaiseAndSetIfChanged(ref _targetFile, value, nameof(TargetFile));
        }

        public bool SliderClicked
        {
            get => _sliderClicked;
            set => this.RaiseAndSetIfChanged(ref _sliderClicked, value, nameof(SliderClicked));
        }

        private AudioEngine _audioEngine;
        private SoundStream _soundStream;

        public double SeekPosition
        {
            get => _seekPosition;
            set { SliderChangedManually(value); }
        }

        private double ValidValuesOnly(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                return 0;
            }
            else return Math.Clamp(value, 0d, 1000d);
        }

        private bool _sliderClicked;
        private double _seekPosition;



        public TrackStatusViewModel TrackStatus
        {
            get { return _trackStatus; }
            set { this.RaiseAndSetIfChanged(ref _trackStatus, value); }
        }
    }
}
