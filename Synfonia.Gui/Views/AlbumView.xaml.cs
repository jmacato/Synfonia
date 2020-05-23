using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Synfonia.Views
{
    public class AlbumView : UserControl
    {
        public AlbumView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}