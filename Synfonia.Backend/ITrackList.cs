using System.Collections.Generic;

namespace Synfonia.Backend
{
    public interface ITrackList
    {
        IList<Track> Tracks { get; }
    }
}