using ReactiveUI;
using Synfonia.Backend;
using System.Reactive.Concurrency;

namespace Synfonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private DiscChanger _model;
        private CollectionExplorerViewModel _collectionExplorer;
        private TrackStatusViewModel _trackStatus;
        private DiscChangerViewModel _discChanger;

        public static MainWindowViewModel Instance { get; set; }

        public MainWindowViewModel(DiscChanger discChanger)
        {
            _model = discChanger;
            DiscChanger = new DiscChangerViewModel(_model);
            TrackStatus = new TrackStatusViewModel(_model);
            CollectionExplorer = new CollectionExplorerViewModel();

            RxApp.MainThreadScheduler.Schedule(async () => await CollectionExplorer.LoadLibrary());
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
