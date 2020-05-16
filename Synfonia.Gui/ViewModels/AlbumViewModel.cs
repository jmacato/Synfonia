using Avalonia;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Synfonia.Backend;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        private bool _coverLoaded;
        private static Nito.AsyncEx.AsyncLock _loadAlbumLock = new Nito.AsyncEx.AsyncLock();

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

            _album.Tracks.ToObservableChangeSet()
                .Transform(x => new TrackViewModel(x))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _tracks)
                .OnItemAdded(async x =>
                {
                    if (!_coverLoaded)
                    {
                        _coverLoaded = true;
                        try
                        {
                            Cover = await LoadCoverAsync();                                                       
                        }
                        catch(Exception e)
                        {
                        }
                    }
                })
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


        public async Task<IBitmap> LoadCoverAsync()
        {
            using (await _loadAlbumLock.LockAsync())
            {
                return await Task.Run(() =>
                {
                    var coverBitmap = _album.LoadCoverArt();

                    if (coverBitmap != null)
                    {
                        using (var ms = new MemoryStream(coverBitmap))
                        {
                            return Bitmap.DecodeToWidth(ms, 200);
                        }
                    }

                    return null;
                });
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
