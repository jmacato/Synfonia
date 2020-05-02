using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Symphony.Views
{
    public class TrackStatusView : UserControl
    {
        public TrackStatusView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
