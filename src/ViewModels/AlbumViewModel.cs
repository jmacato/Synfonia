using Avalonia;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SkiaSharp;
using Synfonia.Backend;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
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
                        Cover = await LoadCoverAsync();
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

        public unsafe Bitmap LoadBitmap(Stream stream)
        {
            var skBitmap = SKBitmap.Decode(stream);

            skBitmap = skBitmap.Resize(new SKImageInfo(400, 400), SKFilterQuality.High);

            fixed (byte* p = skBitmap.Bytes)
            {
                IntPtr ptr = (IntPtr)p;

                return new Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, ptr, new PixelSize(skBitmap.Width, skBitmap.Height), new Vector(96, 96), skBitmap.RowBytes);
            }
        }


        public async Task<Bitmap> LoadCoverAsync ()
        {
            return await Task.Run(async () =>
            {
                var coverBitmap = _album.LoadCoverArt();

                if (coverBitmap != null)
                {
                    using (var ms = new MemoryStream(coverBitmap))
                    {
                        return LoadBitmap(ms);
                    }
                }

                return null;
            });
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
