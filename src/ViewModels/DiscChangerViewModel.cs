using ReactiveUI;
using Synfonia.Backend;
using System.Reactive;
using System.Threading.Tasks;

namespace Synfonia.ViewModels
{
    public class DiscChangerViewModel : ViewModelBase
    {        
        private bool _sliderClicked;        
        private bool _isPaused;
        private DiscChanger _discChanger;

        public DiscChangerViewModel(DiscChanger discChanger)
        {
            _discChanger = discChanger;


            PlayCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                 await _discChanger.Play();
            });

            ForwardCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _discChanger.Forward();
            });

            BackCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _discChanger.Back();
            });
        }

        public async Task LoadTrackList (ITrackList trackList)
        {
            await _discChanger.LoadTrackList(trackList);
        }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }

        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public bool SliderClicked
        {
            get => _sliderClicked;
            set => this.RaiseAndSetIfChanged(ref _sliderClicked, value, nameof(SliderClicked));
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => this.RaiseAndSetIfChanged(ref _isPaused, value, nameof(IsPaused));
        }
    }
}