using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;


namespace Synfonia.Backend
{
    public class Track
    {
        [Key, ForeignKey(nameof(Track))]
        public Guid TrackGuid { get; set; }
 
        public string Title {get;set;}

        public Guid AlbumGuid {get;set;}
        public Album Album { get; set; }

        public string Path { get; set; }
    }
}