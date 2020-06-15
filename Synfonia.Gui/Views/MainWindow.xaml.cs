using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace Synfonia.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            ExtendClientAreaTitleBarHeightHint = -1;
            InitializeComponent();            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachDevTools();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            ExtendClientAreaChromeHints =
                ExtendClientAreaChromeHints.SystemChromeButtons |
                ExtendClientAreaChromeHints.ManagedChromeButtons |
                ExtendClientAreaChromeHints.OSXThickTitleBar;
        }
    }
}