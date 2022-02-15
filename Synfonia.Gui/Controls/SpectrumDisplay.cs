using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using ReactiveUI;
using SkiaSharp;
using Avalonia.Skia.Helpers;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Synfonia.Controls
{
    public class SpectrumDisplay : UserControl, ICustomDrawOperation
    {
        public static readonly DirectProperty<SpectrumDisplay, double[,]> FFTDataProperty =
            AvaloniaProperty.RegisterDirect<SpectrumDisplay, double[,]>(
                nameof(FFTData),
                o => o.FFTData,
                (o, v) => o.FFTData = v);

        private double[,] _averagedData;
        private readonly int _averageLevel = 10;
        private bool _center = false;

        private double[,] _fftData;
        private double _lastStrokeThickness;
        private IPen _linePen;

        public SpectrumDisplay()
        {
            this.GetObservable(FFTDataProperty)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (FFTData != null) FFTData = new double[FFTData.GetLength(0), FFTData.GetLength(1)];
                });

            Clock = new Clock();
            Clock.Subscribe(Tick);
        }

        private void Tick(TimeSpan obj)
        {
            //if (!_isRenderFinished) return;

            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        public double[,] FFTData
        {
            get => _fftData;
            set => SetAndRaise(FFTDataProperty, ref _fftData, value);
        }

        private void RenderBars(IDrawingContextImpl context)
        {
            if (FFTData != null)
            {
                var length = FFTData.GetLength(1);
                var gaps = length * 2 + 1;
                var gapSize = 0.0;
                var binStroke = (Bounds.Width - gaps * gapSize) / (length * 2);
                
                var x = binStroke / 2 + gapSize;

                for (var channel = 0; channel < 2; channel++)
                for (var i = 0; i < length; i++)
                {
                    context.DrawLine(_linePen, new Point(x, Bounds.Height),
                        new Point(x,
                            Bounds.Height * ((1 - (Math.Min(1,
                                _averagedData[channel, channel == 0 ? length - 1 - i : i])) * 0.8))));
                    x += binStroke + gapSize;
                }
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (FFTData != null)
            {
                var length = FFTData.GetLength(1);
                var gaps = length * 2 + 1;

                var gapSize = 0.0;

                var binStroke = (Bounds.Width - gaps * gapSize) / (length * 2);

                if (_lastStrokeThickness != binStroke)
                {
                    _lastStrokeThickness = binStroke;
                    _linePen = new ImmutablePen(Foreground.ToImmutable(), _lastStrokeThickness);
                }

                if (_averagedData is null || FFTData.GetLength(1) != _averagedData.GetLength(1))
                    _averagedData = new double[2, FFTData.GetLength(1)];

                for (var channel = 0; channel < 2; channel++)
                for (var i = 0; i < FFTData.GetLength(1); i++)
                {
                    _averagedData[channel, i] -= _averagedData[channel, i] / _averageLevel;
                    _averagedData[channel, i] += Math.Abs(FFTData[channel, i]) / _averageLevel;
                }

                context.Custom(this);
            }

            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            _center = !_center;
        }

        public void Dispose()
        {
        }

        public bool HitTest(Point p) => Bounds.Contains(p);
        

        public void Render(IDrawingContextImpl context)
        {
            var bounds = Bounds;

            if (context is not ISkiaDrawingContextImpl skia)
            {
                return;
            }
            
            using (var barsLayer = DrawingContextHelper.CreateDrawingContext(bounds.Size, new Vector(96,96), skia.GrContext))
            {
                RenderBars(barsLayer);
                
                using (var filter = SKImageFilter.CreateBlur(24, 24, SKShaderTileMode.Clamp))
                using(var paint = new SKPaint{ ImageFilter = filter})    
                {
                    barsLayer.DrawTo(skia, paint);
                }
            }
        }

        public bool Equals(ICustomDrawOperation? other) => false;
    }
}