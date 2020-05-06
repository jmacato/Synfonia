using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class VolumeControlViewModel
    {
        private DiscChanger discChanger;

        public VolumeControlViewModel(DiscChanger discChanger)
        {
            this.discChanger = discChanger;
        }
    }
}