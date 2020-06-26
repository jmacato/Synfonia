using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using System;
using System.Reactive;
using Avalonia.Media;
using Avalonia.Animation.Animators;
using Avalonia.Threading;
using ReactiveUI;

namespace Synfonia.Controls
{
    public class ScrollingTextBlock : TextBlock
    {
        /// <summary>
        /// Defines the <see cref="TextGap"/> property.
        /// </summary>
        public static readonly StyledProperty<double> TextGapProperty =
            AvaloniaProperty.Register<ScrollingTextBlock, double>(nameof(TextGap), 30d);

        /// <summary>
        /// Defines the <see cref="MarqueeSpeed"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MarqueeSpeedProperty =
            AvaloniaProperty.Register<ScrollingTextBlock, double>(nameof(MarqueeSpeed), 1d);

        /// <summary>
        /// Defines the <see cref="DelayProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<TimeSpan> DelayProperty =
            AvaloniaProperty.Register<ScrollingTextBlock, TimeSpan>(nameof(Delay), TimeSpan.FromSeconds(2));

        public ScrollingTextBlock()
        {
            this.WhenAnyValue(x => x.Text)
                .Subscribe(OnTextChanged);

            // Initialize fields with default values.
            _textGap = TextGap;
            _offsetSpeed = MarqueeSpeed;
            _waitDuration = Delay;
            
            if (Clock is null) Clock = new Clock();
            Clock.Subscribe(Tick);
        }

        private void OnTextChanged(string obj)
        {
            _offset = 0;
            _waiting = true;
            _waitCounter = TimeSpan.Zero;
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        /// <summary>
        /// Gets or sets the gap between animated text.
        /// </summary>
        public double TextGap
        {
            get { return GetValue(TextGapProperty); }
            set
            {
                _textGap = value;
                SetValue(TextGapProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the speed of text scrolling.
        /// </summary>
        public double MarqueeSpeed
        {
            get { return GetValue(MarqueeSpeedProperty); }
            set
            {
                _offsetSpeed = value;
                SetValue(MarqueeSpeedProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the delay between text animations.
        /// </summary>
        public TimeSpan Delay
        {
            get { return GetValue(DelayProperty); }
            set
            {
                _waitDuration = value;
                SetValue(DelayProperty, value);
            }
        }

        private bool _isConstrained;
        private TimeSpan _oldFrameTime;
        private TimeSpan _waitDuration;
        private TimeSpan _waitCounter;

        private bool _waiting = false;
        private bool _animate = false;
        private double _offset;
        private double _offsetSpeed;

        private double _textWidth;
        private double _textHeight;
        private double _textGap;
        private double[] _offsets = new double[3];

        private void Tick(TimeSpan curFrameTime)
        {
            var frameDelta = curFrameTime - _oldFrameTime;
            _oldFrameTime = curFrameTime;

            if (_waiting)
            {
                _waitCounter += frameDelta;

                if (_waitCounter >= _waitDuration)
                {
                    _waitCounter = TimeSpan.Zero;
                    _waiting = false;
                    Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
                }
            }
            else if (_animate)
            {
                _offset += _offsetSpeed;

                if (_offset >= ((_textWidth + _textGap) * 2))
                {
                    _offset = 0;
                    _waiting = true;
                };

                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            }
        }

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
                _textHeight = TextLayout.Bounds.Height;

                var constraints = this.Bounds.Deflate(Padding);
                var constraintsWidth = constraints.Width;
 
                _isConstrained = _textWidth >= constraintsWidth;

                if (_isConstrained & !_waiting)
                {
                    _animate = true;
                    var tOffset = padding.Left - _offset;

                    _offsets[0] = tOffset;
                    _offsets[1] = tOffset + _textWidth + _textGap;
                    _offsets[2] = tOffset + (_textWidth + _textGap) * 2;

                    foreach (var offset in _offsets)
                    {
                        var nR = new Rect(offset, padding.Top, _textWidth, _textHeight);
                        var nC = new Rect(0, padding.Top, constraintsWidth, constraints.Height);

                        if (nC.Intersects(nR))
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