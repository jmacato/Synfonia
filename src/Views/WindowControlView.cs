using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Synfonia.Views
{
    public class WindowControlView : UserControl
    {
        public WindowControlView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
