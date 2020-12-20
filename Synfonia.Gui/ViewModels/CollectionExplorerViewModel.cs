using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class CollectionExplorerViewModel : ViewModelBase
    {
        private ReadOnlyObservableCollection<AlbumViewModel> _albums;
        private SelectArtworkViewModel _selectArtwork;
        private AlbumViewModel _selectedAlbum;
        private bool _isAlbumsEmpty = true;

        public CollectionExplorerViewModel(LibraryManager model, DiscChanger changer)
        {
            SelectArtwork = new SelectArtworkViewModel();

            model.Albums.ToObservableChangeSet()
                .Transform(album => new AlbumViewModel(album, changer))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _albums)
                .OnItemAdded(x =>
                {
                    if (SelectedAlbum is null) SelectedAlbum = x;
                })
                .DisposeMany()
                .Subscribe();

            ScanLibraryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                string musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (string.IsNullOrWhiteSpace(musicFolder))
                    musicFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music");
                
                await Task.Run(async () => await model.ScanMusicFolder(musicFolder));
            });

            model.Albums.ToObservableChangeSet()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Select(x => model.Albums.Count == 0)
                        .Subscribe(x => IsAlbumsEmpty = x);

            RxApp.MainThreadScheduler.Schedule(async () => { await model.LoadLibrary(); });
        }

        public SelectArtworkViewModel SelectArtwork
        {
            get => _selectArtwork;
            set => this.RaiseAndSetIfChanged(ref _selectArtwork, value);
        }

        public bool IsAlbumsEmpty
        {
            get => _isAlbumsEmpty;
            set => this.RaiseAndSetIfChanged(ref _isAlbumsEmpty, value);
        }

        public ReadOnlyObservableCollection<AlbumViewModel> Albums
        {
            get => _albums;
            set => this.RaiseAndSetIfChanged(ref _albums, value);
        }

        public AlbumViewModel SelectedAlbum
        {
            get => _selectedAlbum;
            set => this.RaiseAndSetIfChanged(ref _selectedAlbum, value);
        }

        public ReactiveCommand<Unit, Unit> ScanLibraryCommand { get; }
    }
}
