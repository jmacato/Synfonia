using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;


namespace Synfonia.Backend
{
    public class Track
    {
        public int TrackId { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }

        public int TrackNumber { get; set; }

        public int AlbumId { get; set; }
        public Album Album { get; set; }
    }
}