
using System.Numerics;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using System;
using System.Linq;
using Avalonia.Media;
using System.Collections.Generic;
using Avalonia.Threading;

namespace Symphony.Controls
{
    public class SpectrumDisplay : UserControl
    {
        IPen LinePen = new Pen(new SolidColorBrush(Colors.Red), 1);
        int delay = 0;
        public override void Render(DrawingContext context)
        {
            for (int i = 0; i < _processedFFT.Length; i++)
            {
                context.DrawLine(LinePen, new Point(i, Bounds.Height), new Point(i, Bounds.Height * (1-_processedFFT[i])));
                _processedFFT[i] *= 0.99;
            }

            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            base.Render(context);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(availableSize.Width, availableSize.Height);
        }

        public static readonly DirectProperty<SpectrumDisplay, Complex[]> FFTDataProperty =
            AvaloniaProperty.RegisterDirect<SpectrumDisplay, Complex[]>(
                nameof(FFTData),
                o => o.FFTData,
                (o, v) => o.FFTData = v);

        private Complex[] _fftData;

        public Complex[] FFTData
        {
            get { return _fftData; }
            set { SetAndRaise(FFTDataProperty, ref _fftData, value); }
        }

        private double[] _processedFFT;

        public SpectrumDisplay()
        {
            this.WhenAnyValue(x => x.FFTData)
                .Where(x => x != null)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(x => UpdateFFT(x))
                .Subscribe();

            _processedFFT = new double[bins / binsPerPoint];
        }

        private int bins = 4096;
        private int binsPerPoint = 2;
        private double minDB = -75;
        private void UpdateFFT(Complex[] fftResults)
        {
            Console.WriteLine("fft.");

            for (int n = 0; n < fftResults.Length / 2; n += binsPerPoint)
            {
                // averaging out bins
                double yPos = 0;
                for (int b = 0; b < binsPerPoint; b++)
                {
                    yPos += GetYPosLog(fftResults[n + b]);
                }

                _processedFFT[n / binsPerPoint] = yPos / binsPerPoint;
            }

        }

        private double GetYPosLog(Complex complex)
        {
            double intensityDB = 10 * Math.Log10(complex.Magnitude);
            if (intensityDB < minDB) intensityDB = minDB;
            return Math.Clamp(intensityDB / minDB, 0.0, 1.0);
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
