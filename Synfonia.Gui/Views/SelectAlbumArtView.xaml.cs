using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Synfonia.Views
{
    public class SelectAlbumArtView : UserControl
    {
        public SelectAlbumArtView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}