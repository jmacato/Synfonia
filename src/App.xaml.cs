using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Symphony.ViewModels;
using Symphony.Views;

namespace Symphony
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
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