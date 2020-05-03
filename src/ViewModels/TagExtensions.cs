using System;
using System.IO;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using System.Linq;

namespace Symphony.ViewModels
{
    public static class TagExtensions
    {
        public static IBitmap LoadAlbumCover(this TagLib.Tag tag)
        {
            var cover = tag.Pictures.Where(x => x.Type == TagLib.PictureType.FrontCover).Concat(tag.Pictures).FirstOrDefault();

            if (cover != null)
            {
                using (var ms = new MemoryStream(cover.Data.Data))
                {
                    try
                    {
                        return new Bitmap(ms);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        public static IBitmap LoadAlbumCover(this Album album)
        {
            var firstTrack = album.Tracks.FirstOrDefault();

            if (firstTrack != null)
            {
                using (var file = TagLib.File.Create(firstTrack.Path))
                {
                    var tag = file.Tag;

                    return tag.LoadAlbumCover();
                }
            }

            return null;
        }

    }
}
