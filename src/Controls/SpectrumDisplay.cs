
using System.Numerics;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using System;
using System.Linq;

namespace Symphony.Controls
{
    public class SpectrumDisplay : UserControl
    {
        public static readonly DirectProperty<SpectrumDisplay, Complex[]> FFTDataProperty =
            AvaloniaProperty.RegisterDirect<SpectrumDisplay, Complex[]>(
                nameof(FFTData),
                o => o.FFTData,
                (o, v) => o.FFTData = v);

        private Complex[] _fftData;

        public Complex[] FFTData
        {
            get { return _fftData; }
            set
            {
                Console.WriteLine("got rex");

                SetAndRaise(FFTDataProperty, ref _fftData, value);
            }
        }

        public SpectrumDisplay()
        {
            // this.WhenAnyValue(x => x.FFTData)
            //     .Do(x =>
            //     {
            //         Console.WriteLine("got re");
            //     })
            //     .Subscribe();

        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            e.Handled = false;
        }
        protected override void OnPointerEnter(PointerEventArgs e)
        {
            e.Handled = false;
        }
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            e.Handled = false;
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            e.Handled = false;
        }
    }
}
