using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Synfonia.Backend;
using Synfonia.ViewModels;
using Synfonia.Views;

namespace Synfonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            Name = "Synfonia";            
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dc = new DiscChanger();
                MainViewModel.Instance = new MainViewModel(dc, new LibraryManager());
                desktop.MainWindow = new MainWindow
                {
                    DataContext = MainViewModel.Instance
                };

                desktop.MainWindow.Closing += (sender, e) =>
                {
                    dc.Dispose();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }        
    }
}