using System.Collections.Generic;
using System.Text.RegularExpressions;
using LiteDB;

namespace Synfonia.Backend
{
    public class Artist
    {
        public const string CollectionName = "artists";
        private string _name;

        public int ArtistId { get; set; }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    return "Unknown Artist";

                return Regex.Unescape(_name);
            }
            set => _name = value;
        }


        [BsonRef(Album.CollectionName)] public List<Album> Albums { get; set; } = new List<Album>();
    }
}