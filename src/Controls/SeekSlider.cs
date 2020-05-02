using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ReactiveUI;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System.Reactive;

namespace Symphony.Controls
{
    /// <summary>
    /// A control that lets the user select from a range of values by moving a Thumb control along a Track.
    /// </summary>
    public class SeekSlider : RangeBase
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            ScrollBar.OrientationProperty.AddOwner<SeekSlider>();

        /// <summary>
        /// Defines the <see cref="IsSnapToTickEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSnapToTickEnabledProperty =
            AvaloniaProperty.Register<SeekSlider, bool>(nameof(IsSnapToTickEnabled), false);

        /// <summary>
        /// Defines the <see cref="TickFrequency"/> property.
        /// </summary>
        public static readonly StyledProperty<double> TickFrequencyProperty =
            AvaloniaProperty.Register<SeekSlider, double>(nameof(TickFrequency), 0.0);

        public ReactiveCommand<double, Unit> SeekCommand;

        // Slider required parts
        private Track _track;
        private Button _decreaseButton;
        private Button _increaseButton;

        /// <summary>
        /// Initializes static members of the <see cref="SeekSlider"/> class. 
        /// </summary>
        static SeekSlider()
        {
            OrientationProperty.OverrideDefaultValue(typeof(SeekSlider), Orientation.Horizontal);
            Thumb.DragStartedEvent.AddClassHandler<SeekSlider>((x, e) => x.OnThumbDragStarted(e), RoutingStrategies.Bubble);
            Thumb.DragDeltaEvent.AddClassHandler<SeekSlider>((x, e) => x.OnThumbDragDelta(e), RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<SeekSlider>((x, e) => x.OnThumbDragCompleted(e), RoutingStrategies.Bubble);
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SeekSlider"/> class. 
        /// </summary>
        public SeekSlider()
        {
            UpdatePseudoClasses(Orientation);
        }

        /// <summary>
        /// Gets or sets the orientation of a <see cref="SeekSlider"/>.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="SeekSlider"/> automatically moves the <see cref="Thumb"/> to the closest tick mark.
        /// </summary>
        public bool IsSnapToTickEnabled
        {
            get { return GetValue(IsSnapToTickEnabledProperty); }
            set { SetValue(IsSnapToTickEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the interval between tick marks.
        /// </summary>
        public double TickFrequency
        {
            get { return GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            if (_decreaseButton != null)
            {
                _decreaseButton.Click -= DecreaseClick;
            }

            if (_increaseButton != null)
            {
                _increaseButton.Click -= IncreaseClick;
            }

            _decreaseButton = e.NameScope.Find<Button>("PART_DecreaseButton");
            _track = e.NameScope.Find<Track>("PART_Track");
            _increaseButton = e.NameScope.Find<Button>("PART_IncreaseButton");

            if (_decreaseButton != null)
            {
                _decreaseButton.Click += DecreaseClick;
            }

            if (_increaseButton != null)
            {
                _increaseButton.Click += IncreaseClick;
            }
        }

        private void DecreaseClick(object sender, RoutedEventArgs e)
        {
            ChangeValueBy(-LargeChange);
        }

        private void IncreaseClick(object sender, RoutedEventArgs e)
        {
            ChangeValueBy(LargeChange);
        }
        public static readonly DirectProperty<SeekSlider, double> SeekValueProperty =
            AvaloniaProperty.RegisterDirect<SeekSlider, double>(
                nameof(SeekValue),
                o => o.SeekValue,
                (o, v) => o.SeekValue = v, 0d);

        private double _SeekValue;
        private bool IsSeeking;

        public double SeekValue
        {
            get { return _SeekValue; }
            set { SetAndRaise(SeekValueProperty, ref _SeekValue, value); }
        }

        private void ChangeValueBy(double by)
        {
            if (IsSnapToTickEnabled)
            {
                by = by < 0 ? Math.Min(-TickFrequency, by) : Math.Max(TickFrequency, by);
            }

            var value = Value;
            var next = SnapToTick(Math.Max(Math.Min(value + by, Maximum), Minimum));
            if (next != value)
            {
                Value = next;
            }
        }

        /// <summary>
        /// Called when user start dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragStarted(VectorEventArgs e)
        {
            IsSeeking = true;
        }

        /// <summary>
        /// Called when user dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragDelta(VectorEventArgs e)
        {
            Thumb thumb = e.Source as Thumb;
            if (thumb != null && _track?.Thumb == thumb)
            {
                MoveToNextTick(_track.Value);
            }
        }

        /// <summary>
        /// Called when user stop dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragCompleted(VectorEventArgs e)
        {
            SeekValue = Value;
            IsSeeking = false;
        }

        /// <summary>
        /// Searches for the closest tick and sets Value to that tick.
        /// </summary>
        /// <param name="value">Value that want to snap to closest Tick.</param>
        private void MoveToNextTick(double value)
        {
            var tmp = SnapToTick(Math.Max(Minimum, Math.Min(Maximum, value)));
            if (!IsSeeking)
                Value = tmp;
        }

        /// <summary>
        /// Snap the input 'value' to the closest tick.
        /// </summary>
        /// <param name="value">Value that want to snap to closest Tick.</param>
        private double SnapToTick(double value)
        {
            if (IsSnapToTickEnabled && TickFrequency > 0.0)
            {
                double previous = Minimum + (Math.Round(((value - Minimum) / TickFrequency)) * TickFrequency);
                double next = Math.Min(Maximum, previous + TickFrequency);
                value = value > (previous + next) * 0.5 ? next : previous;
            }

            return value;
        }

        private void UpdatePseudoClasses(Orientation o)
        {
            PseudoClasses.Set(":vertical", o == Orientation.Vertical);
            PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
        }
    }
}
