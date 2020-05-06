using Avalonia.Media.Imaging;
using DynamicData;
using ReactiveUI;
using Synfonia.Backend;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Synfonia.ViewModels
{
    public class AlbumViewModel : ViewModelBase, IComparable<AlbumViewModel>
    {
        private IBitmap _cover;
        private Album _album;
        private ReadOnlyObservableCollection<TrackViewModel> _tracks;

        public AlbumViewModel(Album album, DiscChanger changer)
        {
            _album = album;            

            GetArtworkCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await MainWindowViewModel.Instance.CollectionExplorer.SelectArtwork.QueryAlbumCoverAsync(this);
            });

            LoadAlbumCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await changer.LoadTrackList(album);
            });


            album.Tracks.AsObservableChangeSet()                
                .Transform(x => new TrackViewModel(x))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _tracks)
                .OnItemAdded(x =>
                {
                    if(Cover is null)
                    {
                        ReloadCover();
                    }
                })
                .DisposeMany()
                .Subscribe();
        }

        public string Title => _album.Title;

        public string Artist => _album.Artist.Name;

        public ReadOnlyObservableCollection<TrackViewModel> Tracks
        {
            get { return _tracks; }
            set { this.RaiseAndSetIfChanged(ref _tracks, value); }
        }

        public Album Model => _album;

        public void ReloadCover ()
        {
            var coverBitmap = _album.LoadCoverArt();

            using (var ms = new MemoryStream(coverBitmap))
            {
                Cover = new Bitmap(ms);
            }
        }

        public IBitmap Cover
        {
            get { return _cover; }
            set { this.RaiseAndSetIfChanged(ref _cover, value); }
        }

        public ReactiveCommand<Unit, Unit> GetArtworkCommand { get; }

        public ReactiveCommand<Unit, Unit> LoadAlbumCommand { get; }

        public int CompareTo([AllowNull] AlbumViewModel other)
        {
            return Title.CompareTo(other.Title);
        }
    }
}
