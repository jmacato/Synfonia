using System;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using Synfonia.Backend;

namespace Synfonia.ViewModels
{
    public class WindowControlViewModel : ViewModelBase
    {
        private MainWindowViewModel mainWindowViewModel;

        public WindowControlViewModel(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;
        }

        // public double Volume
        // {
        //     get => _volume;
        //     set => this.RaiseAndSetIfChanged(ref _volume, value);
        // }
    }
}