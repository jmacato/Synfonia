using System.Collections.Generic;

namespace Synfonia.ViewModels
{
    public interface ITrackList
    {
        IList<Track> Tracks { get; }
    }
}