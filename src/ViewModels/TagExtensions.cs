using System;
using System.IO;
using System.Reactive.Linq;
using Id3;
using Avalonia.Media.Imaging;
using System.Linq;

namespace Symphony.ViewModels
{
    public static class TagExtensions
    {
        public static IBitmap LoadAlbumCover(this Id3Tag tag)
        {
            var cover = tag.Pictures.FirstOrDefault(x => x.PictureType == Id3.Frames.PictureType.FrontCover);

            if (cover != null)
            {
                using (var ms = new MemoryStream(cover.PictureData))
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

    }
}
