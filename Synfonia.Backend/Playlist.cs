using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Synfonia.Backend
{
    public class Playlist : ITrackList
    {
        public ObservableCollection<Track> Tracks { get; set; } = new ObservableCollection<Track>();

        IList<Track> ITrackList.Tracks => Tracks;

        public void AddTracks(ITrackList tracks)
        {
            foreach (var track in tracks.Tracks) Tracks.Add(track);
        }
    }
}