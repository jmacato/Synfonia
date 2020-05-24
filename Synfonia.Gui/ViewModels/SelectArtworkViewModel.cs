using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Nito.AsyncEx;
using ReactiveUI;
using Synfonia.Backend.Artwork;

namespace Synfonia.ViewModels
{
    public class SelectArtworkViewModel : ViewModelBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ObservableCollection<CoverViewModel> _covers;
        private AlbumViewModel _currentAlbum;
        private bool _isVisible;
        private readonly AsyncLock _lock = new AsyncLock();
        private CoverViewModel _selectedCover;

        public SelectArtworkViewModel()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Covers = new ObservableCollection<CoverViewModel>();

            this.WhenAnyValue(x => x.SelectedCover)
                .Where(x => x != null)
                .Subscribe(async x =>
                {
                    _cancellationTokenSource.Cancel();
                    IsVisible = false;

                    await _currentAlbum.Model.UpdateCoverArtAsync(x.Url);

                    _currentAlbum.Cover = await _currentAlbum.LoadCoverAsync();

                    IsVisible = false;
                });

            this.WhenAnyValue(x => x.IsVisible)
                .Where(x => !x)
                .Subscribe(async _ =>
                {
                    _cancellationTokenSource?.Cancel();

                    using (await _lock.LockAsync())
                    {
                        Covers.Clear();
                    }
                });
        }

        public ObservableCollection<CoverViewModel> Covers
        {
            get => _covers;
            set => this.RaiseAndSetIfChanged(ref _covers, value);
        }

        public CoverViewModel SelectedCover
        {
            get => _selectedCover;
            set => this.RaiseAndSetIfChanged(ref _selectedCover, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public async Task QueryAlbumCoverAsync(AlbumViewModel album)
        {
            _cancellationTokenSource?.Cancel();

            using (await _lock.LockAsync())
            {
                _cancellationTokenSource = new CancellationTokenSource();

                Covers.Clear();

                IsVisible = true;

                SelectedCover = null;
                _currentAlbum = album;

                var scraper = new AlbumArtworkScraper();

                var artworkDatas = await scraper.GetPossibleAlbumArt("uk", album.Artist, album.Title);

                if (artworkDatas == null)
                {
                    IsVisible = false;

                    return;
                }

                foreach (var artworkData in artworkDatas)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested) return;

                    var clientHandler = new HttpClientHandler();
                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var data = await client.GetByteArrayAsync(artworkData.Url);

                        using (var ms = new MemoryStream(data))
                        {
                            Covers.Add(new CoverViewModel
                            {
                                Title = artworkData.Album,
                                Artist = artworkData.Artist,
                                Url = artworkData.Url,
                                Cover = new Bitmap(ms)
                            });
                        }
                    }
                }
            }
        }
    }
}