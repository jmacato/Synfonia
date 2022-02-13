using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private CollectionExplorerViewModel _collectionExplorer;
        private DiscChangerViewModel _discChanger;
        private TrackStatusViewModel _trackStatus;
        private VolumeControlViewModel _volumeControl;

        public MainViewModel(DiscChanger discChanger, LibraryManager libraryManager)
        {
            DiscChanger = new DiscChangerViewModel(discChanger);
            TrackStatus = new TrackStatusViewModel(discChanger, libraryManager);
            CollectionExplorer = new CollectionExplorerViewModel(libraryManager, discChanger);
            VolumeControl = new VolumeControlViewModel(discChanger);
        }

        public static MainViewModel Instance { get; set; }

        public DiscChangerViewModel DiscChanger
        {
            get => _discChanger;
            set => this.RaiseAndSetIfChanged(ref _discChanger, value);
        }

        public TrackStatusViewModel TrackStatus
        {
            get => _trackStatus;
            set => this.RaiseAndSetIfChanged(ref _trackStatus, value);
        }

        public CollectionExplorerViewModel CollectionExplorer
        {
            get => _collectionExplorer;
            set => this.RaiseAndSetIfChanged(ref _collectionExplorer, value);
        }

        public VolumeControlViewModel VolumeControl
        {
            get => _volumeControl;
            set => this.RaiseAndSetIfChanged(ref _volumeControl, value);
        }
    }
}