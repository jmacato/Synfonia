using System.Collections.Generic;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private CollectionExplorerViewModel _collectionExplorer;
        private DiscChangerViewModel _discChanger;
        private TrackStatusViewModel _trackStatus;
        private VolumeControlViewModel _volumeControl;
        private IList<PanelItem> _panelItems;

        public MainWindowViewModel(DiscChanger discChanger, LibraryManager libraryManager)
        {
            DiscChanger = new DiscChangerViewModel(discChanger);
            TrackStatus = new TrackStatusViewModel(discChanger, libraryManager);
            CollectionExplorer = new CollectionExplorerViewModel(libraryManager, discChanger);
            VolumeControl = new VolumeControlViewModel(discChanger);
            PanelItems = new List<PanelItem>
            {
                new PanelItem() { IconID = "Icon_Hamburger", Title = "Test Menu Item" },
                new PanelItem() { IconID = "Icon_Hamburger", Title = "Test Menu Item" },
                new PanelItem() { IconID = "Icon_Hamburger", Title = "Test Menu Item" },
                new PanelItem() { IconID = "HambIcon_urger", Title = "Test Menu Item" },
            };
        }

        public static MainWindowViewModel Instance { get; set; }

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

        public IList<PanelItem> PanelItems
        {
            get => _panelItems;
            set => this.RaiseAndSetIfChanged(ref _panelItems, value);
        }
    }
}