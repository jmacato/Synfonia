using System;
using SharpAudio.Codec;

namespace Synfonia.Backend
{
    public class TrackContainer : IDisposable
    {
        public TrackContainer(Track track, SoundStream soundStream)
        {
            Track = track;
            SoundStream = soundStream;
        }

        public Track Track { get; }

        public SoundStream SoundStream { get; }

        public void Dispose()
        {
            SoundStream?.Dispose();
        }
    }
}