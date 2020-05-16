using SharpAudio.Codec;
using System;
using System.Reactive.Disposables;

namespace Synfonia.Backend
{
    public class TrackContainer : IDisposable
    {
        private Track _track;
        private SoundStream _soundStream;
        private CompositeDisposable _disp;

        public TrackContainer(Track track, SoundStream soundStream, CompositeDisposable disposable)
        {
            _track = track;
            _soundStream = soundStream;
            _disp = disposable;
        }

        public Track Track => _track;
        public SoundStream SoundStream => _soundStream;

        public void Dispose()
        {
            _disp?.Dispose();
        }
    }
}
