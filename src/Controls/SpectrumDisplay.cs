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
        private IPen _linePen = new Pen(new SolidColorBrush(Colors.LightGray, 0.5), 1);
        private double _lastStrokeThickness;
        private double[] _averagedData;
        private int _averageLevel = 10;

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.DrawRectangle(new Pen(new SolidColorBrush(Colors.Red, 0.5)), new Rect(0, 0, Bounds.Width, Bounds.Height));

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

                var gaps = FFTData.Length - 1;

                var gapSize = 2;
                if ((gaps * gapSize) > Bounds.Width)
                {
                    gapSize = 0;
                }

                var binStroke = (Bounds.Width - (gaps * gapSize)) / FFTData.Length;

                if (_lastStrokeThickness != binStroke)
                {
                    _lastStrokeThickness = binStroke;
                    _linePen = new Pen(new SolidColorBrush(Colors.LightGray, 0.5), _lastStrokeThickness);
                }

                double x = binStroke / 2;
                for (int i = 0; i < _averagedData.Length; i++)
                {
                    context.DrawLine(_linePen, new Point(x, Bounds.Height), new Point(x, Bounds.Height * (1 - _averagedData[i])));
                    x += (binStroke + 2);
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
