using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using System.Diagnostics.CodeAnalysis;
using ReactiveUI;
using Symphony.Scrobbler;
using System.Reactive;
using System.IO;

namespace Symphony.ViewModels
{
    public class Album : ViewModelBase, IComparable<Album>
    {
        private IBitmap _cover;

        public Album()
        {
            Tracks = new List<Track>();

            GetArtworkCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var scraper = new AlbumArtworkScraper();

                var data = await scraper.DownloadArtwork("uk", Artist, Title);

                //TagLib.File.Create()


                using (var ms = new MemoryStream(data))
                {
                    Cover = new Bitmap(ms);
                }
            });
        }

        public string Title { get; set; }

        public string Artist { get; set; }

        public List<Track> Tracks { get; set; }

        public IBitmap Cover
        {
            get { return _cover; }
            set { this.RaiseAndSetIfChanged(ref _cover, value); }
        }

        public ReactiveCommand<Unit, Unit> GetArtworkCommand { get; }

        public int CompareTo([AllowNull] Album other)
        {
            return Title.CompareTo(other.Title);
        }
    }
}
