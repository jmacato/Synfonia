using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class DiscChangerViewModel : ViewModelBase
    {
        private readonly ObservableAsPropertyHelper<bool> _isPaused;
        private readonly DiscChanger _discChanger;
        private bool _sliderClicked;

        public DiscChangerViewModel(DiscChanger discChanger)
        {
            _discChanger = discChanger;

            _isPaused = _discChanger.WhenAnyValue(x => x.IsPaused)
                .ToProperty(this, nameof(IsPaused));

            PlayCommand = ReactiveCommand.CreateFromTask(async () => { await _discChanger.Play(); });

            ForwardCommand = ReactiveCommand.CreateFromTask(async () => { await _discChanger.Forward(); });

            BackCommand = ReactiveCommand.CreateFromTask(async () => { await _discChanger.Back(); });
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

        public async Task AppendTrackList(ITrackList trackList)
        {
            await _discChanger.AppendTrackList(trackList);
        }

        public async Task LoadTrackList(ITrackList trackList)
        {
            await _discChanger.LoadTrackList(trackList);
        }
    }
}