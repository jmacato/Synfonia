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
        public MainWindowViewModel()
        {
            this._audioEngine = AudioEngine.CreateDefault();

            if (_audioEngine == null)
            {
                throw new Exception("Failed to create an audio backend!");
            }

            this.WhenAnyValue(x => x.TargetFile)
                 .DistinctUntilChanged()
                .Subscribe(x => this.TargetFile = x);



            // PlayCommand = ReactiveCommand.CreateFromTask(DoPlay); //this.WhenAnyValue(x => x.TargetFile).Select(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x)));
        }

        // public readonly ReactiveCommand<Unit, Unit> PlayCommand;

        public async Task DoPlay()
        {
            this._soundStream = new SoundStream(File.OpenRead(TargetFile), _audioEngine);

            _soundStream.WhenAnyValue(x => x.Position)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Do(x => this.Duration = _soundStream.Duration)
                        .Do(x => _position = x.TotalSeconds / Duration.TotalSeconds)
                        .Do(x => this.RaisePropertyChanged(nameof(Position)))
                        .Subscribe();



            _soundStream.Play();
        }

        public void SliderChangedManually(double value)
        {
            if (_soundStream is null) return;
            if (!_soundStream.IsPlaying) return;

            var x = ValidValuesOnly(value);
            var z = TimeSpan.FromSeconds(x  * Duration.TotalSeconds);
            _soundStream.TrySeek(z);
        }

        public string Greeting => "Hello World!";

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

        private double _position = 0.0d;

        public double Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value, nameof(Position));
        }

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

        private TimeSpan _duration;
        private bool _sliderClicked;
        private double _seekPosition;

        public TimeSpan Duration
        {
            get => _duration;
            private set => this.RaiseAndSetIfChanged(ref _duration, value, nameof(Duration));
        }


    }
}
