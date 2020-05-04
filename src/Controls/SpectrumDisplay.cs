using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Symphony.Controls
{
    public class SpectrumDisplay : UserControl
    {
        IPen LinePen = new Pen(new SolidColorBrush(Colors.LightGray, 0.5), 1);

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (FFTData != null)
            {
                for (int i = 0; i < FFTData.Length; i++)
                {
                    context.DrawLine(LinePen, new Point(i, Bounds.Height), new Point(i, Bounds.Height * (1 - FFTData[i])));
                    FFTData[i] *= 0.9;
                }

                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (FFTData != null)
            {
                return new Size(FFTData.Length, availableSize.Height);
            }
            else
            {
                return new Size(1, availableSize.Height);
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
    }
}
