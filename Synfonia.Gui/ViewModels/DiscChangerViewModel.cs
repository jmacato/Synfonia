using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class DiscChangerViewModel : ViewModelBase
    {
        private readonly ObservableAsPropertyHelper<bool> _isPlaying;
        private readonly DiscChanger _discChanger;
        private bool _sliderClicked;

        public DiscChangerViewModel(DiscChanger discChanger)
        {
            _discChanger = discChanger;

            _isPlaying = _discChanger.WhenAnyValue(x => x.IsPlaying)
                .ToProperty(this, nameof(IsPlaying));

            PlayCommand = ReactiveCommand.Create(() =>
            {
                if (_discChanger.IsPlaying)
                {
                    _discChanger.Pause();
                }
                else
                {
                    _discChanger.Play();
                }
            });

            ForwardCommand = ReactiveCommand.Create(_discChanger.Forward);

            BackCommand = ReactiveCommand.Create(_discChanger.Back);
        }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }

        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public bool SliderClicked
        {
            get => _sliderClicked;
            set => this.RaiseAndSetIfChanged(ref _sliderClicked, value, nameof(SliderClicked));
        }

        public bool IsPlaying => _isPlaying?.Value ?? false;

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