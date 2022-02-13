using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using JetBrains.Annotations;
using SkiaSharp;

namespace Synfonia.Controls;

public class BlurBehindRenderOperation : ICustomDrawOperation
	{
		private readonly Rect _bounds;

		public BlurBehindRenderOperation(Rect bounds)
		{
			_bounds = bounds;
		}

		public void Dispose()
		{

		}

		public bool HitTest(Point p) => _bounds.Contains(p);


		static SKColorFilter CreateAlphaColorFilter(double opacity)
		{
			if (opacity > 1)
			{
				opacity = 1;
			}

			var c = new byte[256];
			var a = new byte[256];
			for (var i = 0; i < 256; i++)
			{
				c[i] = (byte)i;
				a[i] = (byte)(i * opacity);
			}

			return SKColorFilter.CreateTable(a, c, c, c);
		}

		public void Render(IDrawingContextImpl context)
		{
			if (context is not ISkiaDrawingContextImpl skia)
			{
				return;
			}

			if (!skia.SkCanvas.TotalMatrix.TryInvert(out var currentInvertedTransform))
			{
				return;
			}


			using var backgroundSnapshot = skia.SkSurface.Snapshot();
			using var backdropShader = SKShader.CreateImage(backgroundSnapshot, SKShaderTileMode.Clamp,
				SKShaderTileMode.Clamp, currentInvertedTransform);

			using var blurred = SKSurface.Create(skia.GrContext, false, new SKImageInfo(
				(int)Math.Ceiling(_bounds.Width),
				(int)Math.Ceiling(_bounds.Height), SKImageInfo.PlatformColorType, SKAlphaType.Premul));
			using (var filter = SKImageFilter.CreateBlur(24, 24, SKShaderTileMode.Clamp))
			using (var blurPaint = new SKPaint
			       {
				       Shader = backdropShader,
				       ImageFilter = filter
			       })
			{
				blurred.Canvas.DrawRect(0, 0, (float)_bounds.Width, (float)_bounds.Height, blurPaint);
			}

			using (var blurSnap = blurred.Snapshot())
			using (var blurSnapShader = SKShader.CreateImage(blurSnap))
			using (var blurSnapPaint = new SKPaint
			       {
				       Shader = blurSnapShader,
				       IsAntialias = true
			       })
			{
				skia.SkCanvas.DrawRect(0, 0, (float)_bounds.Width, (float)_bounds.Height, blurSnapPaint);
			}
		}

		public Rect Bounds => _bounds.Inflate(16);

		public bool Equals(ICustomDrawOperation? other)
		{
			return other is BlurBehindRenderOperation op && op._bounds == _bounds;
		}
	}

public class CustomBlurBehind : Control
{
	public override void Render(DrawingContext context)
	{
		context.Custom(new BlurBehindRenderOperation(new Rect(default, Bounds.Size)));
	}
}