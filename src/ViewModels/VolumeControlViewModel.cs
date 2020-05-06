using System;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class VolumeControlViewModel : ViewModelBase
    {
        private DiscChanger discChanger;
        private bool _isMuted;
        private double _volume = 1d;

        public VolumeControlViewModel(DiscChanger discChanger)
        {
            this.discChanger = discChanger;

            Observable.FromEventPattern(discChanger, nameof(discChanger.TrackChanged))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        if (IsMuted)
                        {
                            discChanger.Volume = 0;
                        }
                        else
                        {
                            discChanger.Volume = this.Volume;
                        }
                    });

            this.WhenAnyValue(x => x.Volume,
                              x => x.IsMuted)
                .Where(x => !x.Item2)
                .Subscribe(x => discChanger.Volume = x.Item1);

            this.WhenAnyValue(x => x.IsMuted)
                .Where(x => x)
                .Subscribe(_ => discChanger.Volume = 0);
        }

        public bool IsMuted
        {
            get => _isMuted;
            set => this.RaiseAndSetIfChanged(ref _isMuted, value);
        }

        public double Volume
        {
            get => _volume;
            set => this.RaiseAndSetIfChanged(ref _volume, value);
        }
    }
}