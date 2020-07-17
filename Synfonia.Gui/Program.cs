using Avalonia;
using Avalonia.ReactiveUI;

namespace Synfonia
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .With(new Win32PlatformOptions {AllowEglInitialization = true, UseDeferredRendering = true})
                .With(new X11PlatformOptions {UseGpu = true, WmClass = "Synfonia"})
                .With(new AvaloniaNativePlatformOptions {UseDeferredRendering = true, UseGpu = true})
                .With(new MacOSPlatformOptions {ShowInDock = true})
                .UseReactiveUI()
                .LogToDebug();
        }
    }
}