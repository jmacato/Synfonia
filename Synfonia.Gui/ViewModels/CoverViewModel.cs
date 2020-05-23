using Avalonia.Media.Imaging;
using ReactiveUI;

namespace Synfonia.ViewModels
{
    public class CoverViewModel : ViewModelBase
    {
        private IBitmap _cover;
        private string _url;

        public string Title { get; set; }

        public string Artist { get; set; }

        public IBitmap Cover
        {
            get => _cover;
            set => this.RaiseAndSetIfChanged(ref _cover, value);
        }

        public string Url
        {
            get => _url;
            set => this.RaiseAndSetIfChanged(ref _url, value);
        }
    }
}