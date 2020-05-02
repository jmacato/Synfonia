using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Symphony.Views
{
    public class AudioControlPanel : UserControl
    {
        public AudioControlPanel()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
