using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using Nito.AsyncEx;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class ArtistViewModel : ViewModelBase
    {
        private readonly Artist _artist;
        
        public ArtistViewModel(Artist artist)
        {
            _artist = artist;
        }

        public string Name => _artist.Name;
    }
    
    public class AlbumViewModel : ViewModelBase, IComparable<AlbumViewModel>
    {
        private static readonly AsyncLock _loadAlbumLock = new AsyncLock();
        private IBitmap _cover;
        private bool _coverLoaded;
        private ReadOnlyObservableCollection<TrackViewModel> _tracks;

        public AlbumViewModel(Album album, DiscChanger changer)
        {
            Model = album;

            GetArtworkCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await MainViewModel.Instance.CollectionExplorer.SelectArtwork.QueryAlbumCoverAsync(this);
            });

            LoadAlbumCommand = ReactiveCommand.CreateFromTask(async () => { await changer.LoadTrackList(album); });

            Model.Tracks.ToObservableChangeSet()
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
                        catch (Exception e)
                        {
                        }
                    }
                })
                .Subscribe();
        }

        public string Title => Model.Title;

        public string Artist => Model.Artist.Name;

        public ReadOnlyObservableCollection<TrackViewModel> Tracks
        {
            get => _tracks;
            set => this.RaiseAndSetIfChanged(ref _tracks, value);
        }

        public Album Model { get; }

        public IBitmap Cover
        {
            get => _cover;
            set => this.RaiseAndSetIfChanged(ref _cover, value);
        }

        public ReactiveCommand<Unit, Unit> GetArtworkCommand { get; }

        public ReactiveCommand<Unit, Unit> LoadAlbumCommand { get; }

        public int CompareTo([AllowNull] AlbumViewModel other)
        {
            return Title.CompareTo(other.Title);
        }


        public async Task<IBitmap> LoadCoverAsync()
        {
            using (await _loadAlbumLock.LockAsync())
            {
                return await Task.Run(() =>
                {
                    var coverBitmap = Model.LoadCoverArt();

                    try
                    {
                        if (coverBitmap != null)
                        {
                            using (var ms = new MemoryStream(coverBitmap))
                            {
                                return new Bitmap(ms);//Bitmap.DecodeToWidth(ms, 200));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        
                    }

                    return null;
                });
            }
        }
    }
}