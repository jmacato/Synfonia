using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Synfonia.Backend
{
    public class Artist
    {

        [Key]
        public Guid ArtistGuid { get; set; }

        public string Name { get; set; }

        public List<Album> Albums { get; set; }
    }
}