using ReactiveUI;
using Synfonia.Backend;
using System.Reactive.Concurrency;

namespace Synfonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {        
        private CollectionExplorerViewModel _collectionExplorer;
        private TrackStatusViewModel _trackStatus;
        private DiscChangerViewModel _discChanger;
        private VolumeControlViewModel _volumeControl;
        private WindowControlViewModel _windowControl;

        public static MainWindowViewModel Instance { get; set; }

        public MainWindowViewModel(DiscChanger discChanger, LibraryManager libraryManager)
        {            
            DiscChanger = new DiscChangerViewModel(discChanger);
            TrackStatus = new TrackStatusViewModel(discChanger, libraryManager);
            CollectionExplorer = new CollectionExplorerViewModel(libraryManager, discChanger);  
            VolumeControl = new VolumeControlViewModel(discChanger); 
            WindowControl = new WindowControlViewModel(this); 
        }

        public DiscChangerViewModel DiscChanger
        {
            get { return _discChanger; }
            set { this.RaiseAndSetIfChanged(ref _discChanger, value); }
        }

        public TrackStatusViewModel TrackStatus
        {
            get { return _trackStatus; }
            set { this.RaiseAndSetIfChanged(ref _trackStatus, value); }
        }

        public CollectionExplorerViewModel CollectionExplorer
        {
            get { return _collectionExplorer; }
            set { this.RaiseAndSetIfChanged(ref _collectionExplorer, value); }
        }
        
        public VolumeControlViewModel VolumeControl
        {
            get { return _volumeControl; }
            set { this.RaiseAndSetIfChanged(ref _volumeControl, value); }
        }
        public WindowControlViewModel WindowControl
        {
            get { return _windowControl; }
            set { this.RaiseAndSetIfChanged(ref _windowControl, value); }
        }
    }
}
