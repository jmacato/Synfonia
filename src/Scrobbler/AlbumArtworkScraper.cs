using FuzzyString;
using iTunesSearch.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Symphony.Scrobbler
{
    public class ArtworkData
    {
        public string Url { get; set; }

        public string Album { get; set; }

        public string Artist { get; set; }
    }

    public class AlbumArtworkScraper
    {
        public async Task<List<ArtworkData>> GetPossibleAlbumArt(string country, string artist, string albumName)
        {
            var searchManager = new iTunesSearchManager();

            var artists = await searchManager.GetSongArtistsAsync(artist);

            var foundArtist = artists.Artists.FirstOrDefault();

            if (foundArtist is null)
            {
                return null;
            }

            var albums = await searchManager.GetAlbumsByArtistIdAsync(foundArtist.ArtistId);

            var options = new List<FuzzyStringComparisonOptions>();
            options.Add(FuzzyStringComparisonOptions.UseOverlapCoefficient);
            options.Add(FuzzyStringComparisonOptions.UseLongestCommonSubsequence);
            options.Add(FuzzyStringComparisonOptions.UseLongestCommonSubstring);

            return albums.Albums.Where(x => x.CollectionName != null && albumName.ApproximatelyEquals(x.CollectionName, FuzzyStringComparisonTolerance.Strong, options.ToArray()))
                .Concat(albums.Albums.Where(x => x.CollectionName != null && !x.CollectionName.Contains(albumName)))
                .Concat(albums.Albums.Where(x => x.CollectionName is null))
                .Where(x => !string.IsNullOrWhiteSpace(x.ArtworkUrl100))
                .Select(x => new ArtworkData
                {
                    Url = x.ArtworkUrl100.Replace("100x100bb", "600x600bb"),
                    Album = x.CollectionName,
                    Artist = x.ArtistName
                })
                .Take(20).ToList();
        }

        public async Task<byte[]> DownloadArtwork(string country, string artist, string albumName)
        {
            var searchManager = new iTunesSearchManager();

            var artists = await searchManager.GetSongArtistsAsync(artist);

            var foundArtist = artists.Artists.FirstOrDefault();

            if (foundArtist is null)
            {
                return null;
            }

            var albums = await searchManager.GetAlbumsByArtistIdAsync(foundArtist.ArtistId);

            var album = albums.Albums.FirstOrDefault(x => x.CollectionName != null && x.CollectionName.Contains(albumName) && !string.IsNullOrWhiteSpace(x.ArtworkUrl100));

            if (album != null)
            {
                var artworkUri = album.ArtworkUrl100.Replace("100x100bb", "1000x1000bb");

                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                using (var client = new HttpClient(clientHandler))
                {
                    var data = await client.GetByteArrayAsync(artworkUri);

                    return data;
                }
            }

            return null;
        }
    }
}
