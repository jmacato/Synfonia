using System.ComponentModel;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ReactiveUI;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Avalonia.Controls.Primitives;

namespace Symphony.Behaviors
{
    public class PointerPressReleaseToBooleanBehavior : Behavior<Slider>
    {

        public static readonly DirectProperty<PointerPressReleaseToBooleanBehavior, bool> IsClickedProperty =
    AvaloniaProperty.RegisterDirect<PointerPressReleaseToBooleanBehavior, bool>(
        nameof(IsClicked),
        o => o.IsClicked,
        (o, v) => o.IsClicked = v);

        private bool _IsClicked;
        public bool IsClicked
        {
            get { return _IsClicked; }
            set { SetAndRaise(IsClickedProperty, ref _IsClicked, value); }
        }


        protected override void OnAttached()
        {
            AssociatedObject.PointerPressed += delegate
            {

            };

            base.OnAttached();
        }

        private void PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            IsClicked = false;
        }

        private void PointerPress(object sender, PointerPressedEventArgs e)
        {
            IsClicked = true;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PointerPressed -= PointerPress;
            AssociatedObject.PointerReleased -= PointerReleased;

            base.OnDetaching();
        }
    }
}