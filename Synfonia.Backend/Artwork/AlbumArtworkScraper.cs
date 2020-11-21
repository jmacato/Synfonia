using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FuzzyString;
using iTunesSearch.Library;

namespace Synfonia.Backend.Artwork
{
    public class AlbumArtworkScraper
    {
        public async Task<List<ArtworkData>> GetPossibleAlbumArt(string country, string artist, string albumName)
        {
            var searchManager = new iTunesSearchManager();

            var artists = await searchManager.GetSongArtistsAsync(artist);

            var foundArtist = artists.Artists.FirstOrDefault();

            if (foundArtist is null) return null;

            var albums = await searchManager.GetAlbumsByArtistIdAsync(foundArtist.ArtistId);

            var options = new List<FuzzyStringComparisonOptions>
            {
                FuzzyStringComparisonOptions.UseOverlapCoefficient,
                FuzzyStringComparisonOptions.UseLongestCommonSubsequence,
                FuzzyStringComparisonOptions.UseLongestCommonSubstring
            };

            var bestMatches = albums.Albums.Where(x =>
                x.CollectionName != null && albumName.ApproximatelyEquals(x.CollectionName,
                    FuzzyStringComparisonTolerance.Strong, options.ToArray())).ToList();

            return bestMatches.Concat(albums.Albums.Where(x => x.CollectionName != null && !bestMatches.Contains(x)))
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

            if (foundArtist is null) return null;

            var albums = await searchManager.GetAlbumsByArtistIdAsync(foundArtist.ArtistId);

            var album = albums.Albums.FirstOrDefault(x =>
                x.CollectionName != null && x.CollectionName.Contains(albumName) &&
                !string.IsNullOrWhiteSpace(x.ArtworkUrl100));

            if (album != null)
            {
                var artworkUri = album.ArtworkUrl100.Replace("100x100bb", "1000x1000bb");

                var clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = delegate { return true; }
                };

                using var client = new HttpClient(clientHandler);

                var data = await client.GetByteArrayAsync(artworkUri);

                return data;
            }

            return null;
        }
    }
}