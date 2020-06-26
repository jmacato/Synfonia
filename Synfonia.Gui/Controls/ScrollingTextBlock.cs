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

                if (_offset >= _textWidth + _textGap)
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
                _isConstrained = _textWidth >= this.Bounds.Deflate(Padding).Width;

                if (_isConstrained)
                {
                    _animate = true;
                    var tOffset = padding.Left - _offset;
                    TextLayout.Draw(context.PlatformImpl, new Point(tOffset, padding.Top));
                    TextLayout.Draw(context.PlatformImpl, new Point(tOffset + _textWidth + _textGap, padding.Top));
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