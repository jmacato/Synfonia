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
    public class SeekSlider : RangeBase
    { 
        // Slider required parts
        private Track _track;
        private SeekTrackButton _decreaseButton;
        private SeekTrackButton _increaseButton;

        /// <summary>
        /// Initializes static members of the <see cref="SeekSlider"/> class. 
        /// </summary>
        static SeekSlider()
        {
            Thumb.DragStartedEvent.AddClassHandler<SeekSlider>((x, e) => x.OnThumbDragStarted(e), RoutingStrategies.Bubble);
            Thumb.DragDeltaEvent.AddClassHandler<SeekSlider>((x, e) => x.OnThumbDragDelta(e), RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<SeekSlider>((x, e) => x.OnThumbDragCompleted(e), RoutingStrategies.Bubble);
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="SeekSlider"/> class. 
        /// </summary>
        public SeekSlider()
        {
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        { 
            _decreaseButton = e.NameScope.Find<SeekTrackButton>("PART_DecreaseButton");
            _track = e.NameScope.Find<Track>("PART_Track");
            _increaseButton = e.NameScope.Find<SeekTrackButton>("PART_IncreaseButton");

            if (_decreaseButton != null)
            {
                _decreaseButton.PointerPressed += DC_PP;
            }

            if (_increaseButton != null)
            {
                _increaseButton.PointerPressed += IC_PP;
            }
        }

        private void IC_PP(object sender, PointerPressedEventArgs e)
        {
            var x = e.GetCurrentPoint(_track);
            SeekValue = x.Position.X / _track.Bounds.Width;
        }

        private void DC_PP(object sender, PointerPressedEventArgs e)
        {
            var x = e.GetCurrentPoint(_track);
            SeekValue = x.Position.X / _track.Bounds.Width;
        }

        public static readonly DirectProperty<SeekSlider, double> SeekValueProperty =
            AvaloniaProperty.RegisterDirect<SeekSlider, double>(
                nameof(SeekValue),
                o => o.SeekValue,
                (o, v) => o.SeekValue = v, 0d);

        private double _SeekValue;

        public double SeekValue
        {
            get { return _SeekValue; }
            set { SetAndRaise(SeekValueProperty, ref _SeekValue, value); }
        }

        public static readonly DirectProperty<SeekSlider, bool> IsSeekingProperty =
            AvaloniaProperty.RegisterDirect<SeekSlider, bool>(
                nameof(IsSeeking),
                o => o.IsSeeking,
                (o, v) => o.IsSeeking = v, false);

        private bool _IsSeeking;

        public bool IsSeeking
        {
            get { return _IsSeeking; }
            set { SetAndRaise(IsSeekingProperty, ref _IsSeeking, value); }
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
            if (e.Source is Thumb thumb && _track?.Thumb == thumb)
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
            Value = Math.Max(Minimum, Math.Min(Maximum, value));
        }
    }
}
