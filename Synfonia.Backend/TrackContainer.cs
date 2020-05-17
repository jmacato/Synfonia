using SharpAudio.Codec;
using System;
using System.Reactive.Disposables;

namespace Synfonia.Backend
{
    public class TrackContainer : IDisposable
    {
        private Track _track;
        private SoundStream _soundStream;

        public TrackContainer(Track track, SoundStream soundStream)
        {
            _track = track;
            _soundStream = soundStream;
        }

        public Track Track => _track;
        public SoundStream SoundStream => _soundStream;

        public void Dispose()
        {
            _soundStream?.Dispose();
        }
    }
}
