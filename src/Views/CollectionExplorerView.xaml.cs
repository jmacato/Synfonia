using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Symphony.Views
{
    public class CollectionExplorerView : UserControl
    {
        public CollectionExplorerView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
