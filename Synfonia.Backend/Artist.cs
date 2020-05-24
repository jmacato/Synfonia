using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Synfonia.Backend
{
    public class Artist
    {
        public int ArtistId { get; set; }
        public string Name { get; set; }
 
        public ObservableCollection<Album> Albums { get; set; } = new ObservableCollection<Album>();
    }
}