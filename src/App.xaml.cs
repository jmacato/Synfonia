using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
                MainWindowViewModel.Instance = new MainWindowViewModel();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = MainWindowViewModel.Instance,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}