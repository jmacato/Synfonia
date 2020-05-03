using Avalonia.Media.Imaging;
using Nito.AsyncEx;
using ReactiveUI;
using Symphony.Scrobbler;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Symphony.ViewModels
{
    public class SelectArtworkViewModel : ViewModelBase
    {
        private bool _isVisible;
        private ObservableCollection<CoverViewModel> _covers;
        private CancellationTokenSource _cancellationTokenSource;
        private AsyncLock _lock = new AsyncLock();
        private CoverViewModel _selectedCover;
        private AlbumViewModel _currentAlbum;

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

                    await _currentAlbum.UpdateCoverArt(x.Url.Replace("600x600bb", "1000x1000bb"));

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
            get { return _covers; }
            set { this.RaiseAndSetIfChanged(ref _covers, value); }
        }

        public CoverViewModel SelectedCover
        {
            get { return _selectedCover; }
            set { this.RaiseAndSetIfChanged(ref _selectedCover, value); }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set { this.RaiseAndSetIfChanged(ref _isVisible, value); }
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
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    var clientHandler = new HttpClientHandler();
                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

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
