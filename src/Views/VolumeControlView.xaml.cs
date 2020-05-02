using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Symphony.Views
{
    public class VolumeControlView : UserControl
    {
        public VolumeControlView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
