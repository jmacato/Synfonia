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
    public class AlbumArtworkScraper
    {
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

            foreach (var album in albums.Albums)
            {
                if (album.CollectionViewUrl != null)
                {
                    Debug.WriteLine(album.CollectionViewUrl);
                }

                if (album.ArtistViewUrl != null)
                {
                    Debug.WriteLine(album.ArtistViewUrl);
                }

                if (album.ArtworkUrl100 != null)
                {
                    Debug.WriteLine(album.ArtworkUrl100);

                    var artworkUri = album.ArtworkUrl100.Replace("100x100bb", "1000x1000bb");

                    HttpClientHandler clientHandler = new HttpClientHandler();
                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var data = await client.GetByteArrayAsync(artworkUri);

                        return data;
                    }
                }
            }

            return null;
        }
    }
}
