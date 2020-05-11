using ReactiveUI;
using Synfonia.Backend;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Synfonia.ViewModels
{
    public class DiscChangerViewModel : ViewModelBase
    {
        private bool _sliderClicked;
        private DiscChanger _discChanger;
        private readonly ObservableAsPropertyHelper<bool> _isPaused;

        public DiscChangerViewModel(DiscChanger discChanger)
        {
            _discChanger = discChanger;

            _isPaused = _discChanger.WhenAnyValue(x => x.IsPaused)
                                    .ToProperty(this, nameof(IsPaused));

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
        
        public async Task AppendTrackList (ITrackList trackList)
        {
            await _discChanger.AppendTrackList(trackList);
        }

        public async Task LoadTrackList(ITrackList trackList)
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

        public bool IsPaused => _isPaused?.Value ?? false;
    }
}