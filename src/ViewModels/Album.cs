﻿using LiteDB;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Symphony.ViewModels
{
    public class Album : ITrackList
    {
        public const string CollectionName = "albums";

        private string _title;

        public int AlbumId { get; set; }

        public string Title
        {
            get => Regex.Unescape(_title);
            set { _title = value; }
        }

        [BsonRef(Track.CollectionName)]
        public IList<Track> Tracks { get; set; } = new List<Track>();
    }
}
