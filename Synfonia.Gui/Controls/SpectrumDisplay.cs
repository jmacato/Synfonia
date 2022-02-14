﻿using System;
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
        private readonly int _averageLevel = 5;
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
            {
                if (FFTData != null)
                {
                    if (_averagedData is null || FFTData.GetLength(1) != _averagedData.GetLength(1))
                        _averagedData = new double[2, FFTData.GetLength(1)];

                    for (var channel = 0; channel < 2; channel++)
                    for (var i = 0; i < FFTData.GetLength(1); i++)
                    {
                        _averagedData[channel, i] -= _averagedData[channel, i] / _averageLevel;
                        _averagedData[channel, i] += Math.Abs(FFTData[channel, i]) / _averageLevel;
                    }

                    var length = FFTData.GetLength(1);
                    var gaps = length * 2 + 1;

                    var gapSize = 0.0;

                    var binStroke = (Bounds.Width - gaps * gapSize) / (length * 2);


                    var x = binStroke / 2 + gapSize;

                    var center = Bounds.Width / 2;

                    for (var channel = 0; channel < 2; channel++)
                    for (var i = 0; i < length; i++)
                    {
                        var dCenter = Math.Abs(x - center);
                        var multiplier = 1 - (dCenter / center);


                        {
                            context.DrawLine(_linePen, new Point(x, Bounds.Height),
                                new Point(x,
                                    Bounds.Height * ((1 - (Math.Min(1,
                                        _averagedData[channel, channel == 0 ? length - 1 - i : i])) * 0.9))));
                            x += binStroke + gapSize;
                        }
                    }
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

        private static ISkiaDrawingContextImpl CreateRenderLayer(GRContext grContext, Size size)
        {
            var surface = SKSurface.Create(grContext, false, new SKImageInfo(
                (int)Math.Ceiling(size.Width),
                (int)Math.Ceiling(size.Height), SKImageInfo.PlatformColorType, SKAlphaType.Premul));

            return new Avalonia.Skia.DrawingContextImpl(new DrawingContextImpl.CreateInfo
            {
                Canvas = surface.Canvas,
                Surface = surface,
                Dpi = new Vector(96, 96),
                VisualBrushRenderer = null,
                DisableTextLcdRendering = false,
                GrContext = grContext
            }, Disposable.Create(() => surface.Dispose()));
        }

        private static void PaintRenderLayer(ISkiaDrawingContextImpl source, ISkiaDrawingContextImpl destination,
            Size size, SKImageFilter imageFilter = null)
        {
            using (var blurSnap = source.SkSurface.Snapshot())
            using (var blurSnapShader = SKShader.CreateImage(blurSnap))
            using (var blurSnapPaint = new SKPaint
                   {
                       Shader = blurSnapShader,
                       ImageFilter = imageFilter,
                       IsAntialias = true
                   })
            {
                destination.SkCanvas.DrawRect(0, 0, (float)size.Width, (float)size.Height, blurSnapPaint);
            }
        }

        public void Render(IDrawingContextImpl context)
        {
            var _bounds = Bounds;

            if (context is not ISkiaDrawingContextImpl skia)
            {
                return;
            }

            if (!skia.SkCanvas.TotalMatrix.TryInvert(out var currentInvertedTransform))
            {
                return;
            }


            using (var barsLayer = CreateRenderLayer(skia.GrContext, _bounds.Size))
            {
                RenderBars(barsLayer);

                using (var filter = SKImageFilter.CreateBlur(24, 24, SKShaderTileMode.Clamp))
                {
                    PaintRenderLayer(barsLayer, skia, _bounds.Size, filter);
                }
            }
        }

        public bool Equals(ICustomDrawOperation? other) => false;
    }
}