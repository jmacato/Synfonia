using ReactiveUI;

namespace Symphony.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private CollectionExplorerViewModel _collectionExplorer;
        private TrackStatusViewModel _trackStatus;
        private DiscChangerViewModel _discChanger;

        public static MainWindowViewModel Instance { get; set; }

        public MainWindowViewModel()
        {
            DiscChanger = new DiscChangerViewModel();
            TrackStatus = new TrackStatusViewModel();
            CollectionExplorer = new CollectionExplorerViewModel();

            CollectionExplorer.LoadLibrary();
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
    }
}
