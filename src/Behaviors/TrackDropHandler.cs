using Avalonia.Input;

namespace Synfonia.Behaviors
{
    /// <summary>
    /// Project editor drop handler.
    /// </summary>
    public class TrackDropHandler : DefaultDropHandler
    {
        /// <inheritdoc/>
        public override bool Validate(object sender, DragEventArgs e, object sourceContext, object targetContext, object state)
        {
            if(sourceContext is Synfonia.ViewModels.AlbumViewModel avm && avm.Model is Synfonia.Backend.ITrackList tl && targetContext is Synfonia.ViewModels.TrackStatusViewModel ts)
            {                
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override bool Execute(object sender, DragEventArgs e, object sourceContext, object targetContext, object state)
        {
            if (sourceContext is Synfonia.ViewModels.AlbumViewModel avm && avm.Model is Synfonia.Backend.ITrackList tl && targetContext is Synfonia.ViewModels.TrackStatusViewModel ts)
            {
                avm.LoadAlbumCommand.Execute();
                return true;
            }
            return false;
        }
    }
}
