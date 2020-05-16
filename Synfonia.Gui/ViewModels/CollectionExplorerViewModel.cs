using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Synfonia.Backend;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Synfonia.ViewModels
{
    public class CollectionExplorerViewModel : ViewModelBase
    {
        private ReadOnlyObservableCollection<AlbumViewModel> _albums;
        private AlbumViewModel _selectedAlbum;
        private SelectArtworkViewModel _selectArtwork;        

        public CollectionExplorerViewModel(LibraryManager model, DiscChanger changer)
        {
            SelectArtwork = new SelectArtworkViewModel();

            model.Albums.ToObservableChangeSet()
                .Transform(album => new AlbumViewModel(album, changer))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _albums)
                .OnItemAdded(x =>
                {
                    if(SelectedAlbum is null)
                    {
                        SelectedAlbum = x;
                    }
                })
                .DisposeMany()
                .Subscribe();

            ScanLibraryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.Run(async ()=> await model.ScanMusicFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))));
            });

            RxApp.MainThreadScheduler.Schedule(async () =>
            {
                await model.LoadLibrary();
            });
        }

        public SelectArtworkViewModel SelectArtwork
        {
            get { return _selectArtwork; }
            set { this.RaiseAndSetIfChanged(ref _selectArtwork, value); }
        }

        public ReadOnlyObservableCollection<AlbumViewModel> Albums
        {
            get { return _albums; }
            set { this.RaiseAndSetIfChanged(ref _albums, value); }
        }

        public AlbumViewModel SelectedAlbum
        {
            get { return _selectedAlbum; }
            set { this.RaiseAndSetIfChanged(ref _selectedAlbum, value); }
        }        

        public ReactiveCommand<Unit, Unit> ScanLibraryCommand { get; }
    }
}
