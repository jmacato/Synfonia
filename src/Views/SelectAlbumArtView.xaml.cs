using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Symphony.Views
{
    public class SelectAlbumArtView : UserControl
    {
        public SelectAlbumArtView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
