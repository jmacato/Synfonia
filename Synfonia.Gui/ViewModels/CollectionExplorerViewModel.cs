using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private ReadOnlyObservableCollection<ArtistViewModel> _artists;
        private ReadOnlyObservableCollection<AlbumViewModel> _albums;

        private SelectArtworkViewModel _selectArtwork;
        private AlbumViewModel _selectedAlbum;
        private bool _isAlbumsEmpty = true;

        public CollectionExplorerViewModel(LibraryManager model, DiscChanger changer)
        {
            SelectArtwork = new SelectArtworkViewModel();
            Tracks = new ObservableCollection<TrackViewModel>();

            Observable.FromEventPattern<PropertyChangedEventArgs>(changer, nameof(changer.PropertyChanged))
                .Where(x => x.EventArgs.PropertyName == nameof(changer.TrackList))
                .Subscribe(x =>
                {
                    Tracks.Clear();

                    foreach (var track in changer.TrackList.Tracks)
                    {
                        Tracks.Add(new TrackViewModel(track));
                    }
                });

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

            model.Artists.ToObservableChangeSet()
                .Transform(album => new ArtistViewModel(album))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _artists)
                .DisposeMany()
                .Subscribe();

            ScanLibraryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.Run(async () => await model.ScanMusicFolder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music")));
            });

            model.Albums.ToObservableChangeSet()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Select(x => model.Albums.Count == 0)
                        .Subscribe(x => IsAlbumsEmpty = x);

            RxApp.MainThreadScheduler.Schedule(async () => { await model.LoadLibrary(); });
        }
        
        public ObservableCollection<TrackViewModel> Tracks { get; } 

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
        
        public ReadOnlyObservableCollection<ArtistViewModel> Artists
        {
            get => _artists;
            set => this.RaiseAndSetIfChanged(ref _artists, value);
        }

        public AlbumViewModel SelectedAlbum
        {
            get => _selectedAlbum;
            set => this.RaiseAndSetIfChanged(ref _selectedAlbum, value);
        }

        public ReactiveCommand<Unit, Unit> ScanLibraryCommand { get; }
    }
}
