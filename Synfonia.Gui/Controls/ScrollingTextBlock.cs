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
        private Size _constraints = Size.Empty;

        public ScrollingTextBlock()
        {
            AffectsRender<ScrollingTextBlock>(BoundsProperty);
            Clock = new Clock();
            Clock.Subscribe(Tick);
        }

        private void Tick(TimeSpan x)
        {
            if (_animate)
            {
                if(_offset == 0)
                {

                }

                _offset += 1;
                _offset %= _textWidth + _textGap;
                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            }
        }

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
                _isConstrained = _textWidth >= this.Bounds.Width;

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

        protected override Size MeasureOverride(Size availableSize)
        {
            this._constraints = availableSize;
            return base.MeasureOverride(availableSize);
        }

    }
}