using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text;
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
            get { return _cover; }
            set { this.RaiseAndSetIfChanged(ref _cover, value); }
        }

        public string Url
        {
            get { return _url; }
            set { this.RaiseAndSetIfChanged(ref _url, value); }
        }
    }
}
