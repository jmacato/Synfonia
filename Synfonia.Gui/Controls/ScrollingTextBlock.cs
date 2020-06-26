using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using System;
using System.Reactive;
using Avalonia.Media;
using Avalonia.Animation.Animators;
using Avalonia.Threading;

namespace Synfonia.Controls
{
    public class ScrollingTextBlock : TextBlock
    {
        private bool _isConstrained;

        public ScrollingTextBlock()
        {
            Clock = new Clock();
            Clock.Subscribe(Tick);
        }

        private void Tick(TimeSpan x)
        {
            var frameDelta = x - oldFrameTime;
            oldFrameTime = x;

            if (_waiting)
            {
                WaitCounter += frameDelta;

                if (WaitCounter >= WaitDuration)
                {
                    WaitCounter = TimeSpan.Zero;
                    _waiting = false;
                }
            }
            else if (_animate)
            {
                _offset += 1;

                if (_offset >= ((_textWidth + _textGap) * 2))
                {
                    _offset = 0;
                    _waiting = true;
                };

                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            }
        }

        private TimeSpan oldFrameTime = TimeSpan.Zero;
        private bool _waiting = false;
        private bool _animate = false;
        private double _offset;
        private TimeSpan WaitDuration = TimeSpan.FromSeconds(2);
        private TimeSpan WaitCounter;

        private double _textWidth;
        private double _textGap = 40;


        public override void Render(DrawingContext context)
        {
            var background = Background;

            if (background != null)
            {
                context.FillRectangle(background, new Rect(Bounds.Size));
            }

            var padding = Padding;

            if (TextLayout != null)
            {
                _textWidth = TextLayout.Bounds.Width;
                var containerWidth = this.Bounds.Deflate(Padding).Width;
                _isConstrained = _textWidth >= containerWidth;

                if (_isConstrained)
                {
                    _animate = true;
                    var tOffset = padding.Left - _offset;

                    var _1stOffset = tOffset;
                    var _2ndOffset = tOffset + _textWidth + _textGap;
                    var _3rdOffset = tOffset + (_textWidth + _textGap) * 2;

                    double[] offsets = new double[] { _1stOffset, _2ndOffset, _3rdOffset };

                    foreach (var offset in offsets)
                    {
                        var renderTextWidth = (offset + _textWidth);
                        if (renderTextWidth >= 0d || renderTextWidth <= containerWidth)
                            TextLayout.Draw(context.PlatformImpl, new Point(offset, padding.Top));
                    }
                }
                else
                {
                    _animate = false;
                    TextLayout.Draw(context.PlatformImpl, new Point(padding.Left, padding.Top));
                }
            }
        }
    }
}