using LiteDB;
using System.Text.RegularExpressions;

namespace Synfonia.Backend
{
    public class Track
    {
        public const string CollectionName = "tracks";
        private string _path;
        private string _title;

        public int TrackId { get; set; }

        public string Title
        {
            get { return Regex.Unescape(_title); }
            set { _title = value; }
        }

        [BsonIgnore]
        public Album Album { get; set; }

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
    }
}
