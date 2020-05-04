using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Symphony.Controls
{
    public class SpectrumDisplay : UserControl
    {
        IPen LinePen = new Pen(new SolidColorBrush(Colors.Red), 1);

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (FFTData != null)
            {
                for (int i = 0; i < FFTData.Length; i++)
                {
                    context.DrawLine(LinePen, new Point(i, Bounds.Height), new Point(i, Bounds.Height * (1 - FFTData[i])));
                }
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
            AffectsRender<SpectrumDisplay>(FFTDataProperty);
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
