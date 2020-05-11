using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Synfonia.Backend
{
    public class Playlist : ITrackList
    {
        public Playlist()
        {
            Tracks = new ObservableCollection<Track>();
        }

        public ObservableCollection<Track> Tracks { get; set; }

        public void AddTracks(ITrackList tracks)
        {
            foreach(var track in tracks.Tracks)
            {
                Tracks.Add(track);
            }
        }

        IList<Track> ITrackList.Tracks => Tracks;
    }
}
