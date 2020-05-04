using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Symphony.Controls
{
    public class SpectrumDisplay : UserControl
    {
        private IPen _linePen;
        private double _lastStrokeThickness;
        private double[] _averagedData;
        private int _averageLevel = 6;

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.FillRectangle(Brushes.White, Bounds);

            if (FFTData != null)
            {
                if (_averagedData is null || FFTData.Length != _averagedData.Length)
                {
                    _averagedData = new double[FFTData.Length];
                }

                for (int i = 0; i < FFTData.Length; i++)
                {
                    _averagedData[i] -= _averagedData[i] / _averageLevel;
                    _averagedData[i] += FFTData[i] / _averageLevel;
                }

                var length = FFTData.Length / 24;
                var gaps = length - 1;

                var gapSize = 1;
                if ((gaps * gapSize) > Bounds.Width)
                {
                    gapSize = 0;
                }

                var binStroke = (Bounds.Width - (gaps * gapSize)) / length;

                if (_lastStrokeThickness != binStroke)
                {
                    _lastStrokeThickness = binStroke;
                    _linePen = new Pen(new SolidColorBrush(Colors.Gray, 0.5), _lastStrokeThickness);
                }

                double x = binStroke / 2;
                for (int i = 0; i < length; i++)
                {
                    context.DrawLine(_linePen, new Point(x, Bounds.Height), new Point(x, Bounds.Height * (1 - _averagedData[i])));
                    x += (binStroke + gapSize);
                }

                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            }
        }

        public static readonly DirectProperty<SpectrumDisplay, double[]> FFTDataProperty =
            AvaloniaProperty.RegisterDirect<SpectrumDisplay, double[]>(
                nameof(FFTData),
                o => o.FFTData,
                (o, v) => o.FFTData = v);

        private double[] _fftData;

        public double[] FFTData
        {
            get { return _fftData; }
            set => SetAndRaise(FFTDataProperty, ref _fftData, value);
        }

        static SpectrumDisplay()
        {
            AffectsMeasure<SpectrumDisplay>(FFTDataProperty);
        }

        public SpectrumDisplay()
        {
            this.GetObservable(FFTDataProperty)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (FFTData != null)
                    {
                        FFTData = new double[FFTData.Length];
                    }
                });
        }
    }
}
