using System.Collections.Generic;

namespace Symphony.ViewModels
{
    public interface ITrackList
    {
        IList<Track> Tracks { get; }
    }
}