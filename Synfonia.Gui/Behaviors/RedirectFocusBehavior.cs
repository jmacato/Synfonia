using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Avalonia.Input;

namespace Synfonia.Behaviors
{
    public class RedirectFocusBehavior : Behavior<Control>
    {
        public static readonly DirectProperty<RedirectFocusBehavior, Control> TargetControlProperty =
       AvaloniaProperty.RegisterDirect<RedirectFocusBehavior, Control>(nameof(TargetControl),
                                                                                 o => o.TargetControl,
                                                                                 (o, v) => o.TargetControl = v);

        private Control _targetControl;

        public Control TargetControl
        {
            get { return _targetControl; }
            set { SetAndRaise(TargetControlProperty, ref _targetControl, value); }
        }

        protected override void OnAttached()
        {
            AssociatedObject.PointerPressed += OnPointerPressed;
            base.OnAttached();
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            TargetControl?.Focus();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PointerPressed -= OnPointerPressed;
            base.OnDetaching();
        }
    }
}