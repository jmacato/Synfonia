using System.Text.RegularExpressions;
using LiteDB;

namespace Synfonia.Backend
{
    public class Track
    {
        public const string CollectionName = "tracks";
        private string _title;

        public int TrackId { get; set; }

        public uint TrackNumber { get; set; }

        public string Title
        {
            get => Regex.Unescape(_title);
            set => _title = value;
        }

        [BsonIgnore]
        public Album Album { get; set; }

        public string Path { get; set; }
    }
}