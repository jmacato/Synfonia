using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class TrackViewModel
    {
        private Track _track;

        public TrackViewModel(Track track)
        {
            _track = track;
        }

        public string Title => _track.Title;

        public Track Model => _track;
    }
}
