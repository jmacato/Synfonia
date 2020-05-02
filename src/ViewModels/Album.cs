using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using System.Diagnostics.CodeAnalysis;

namespace Symphony.ViewModels
{
    public class Album : IComparable<Album>
    {
        public Album()
        {
            Tracks = new List<Track>();
        }

        public string Title { get; set; }

        public List<Track> Tracks { get; set; }

        public IBitmap Cover { get; set; }

        public int CompareTo([AllowNull] Album other)
        {
            return Title.CompareTo(other.Title);
        }
    }
}
