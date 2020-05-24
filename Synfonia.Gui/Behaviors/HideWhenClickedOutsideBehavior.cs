using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Synfonia.Behaviors
{
    public class HideWhenClickedOutsideBehavior : Behavior<Control>
    {
        public static readonly DirectProperty<HideWhenClickedOutsideBehavior, Control> HitTargetProperty =
            AvaloniaProperty.RegisterDirect<HideWhenClickedOutsideBehavior, Control>(
                nameof(HitTarget),
                o => o.HitTarget,
                (o, v) => o.HitTarget = v);

        private IDisposable _disposable;
        private Control _hitTarget;

        public Control HitTarget
        {
            get => _hitTarget;
            set => SetAndRaise(HitTargetProperty, ref _hitTarget, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AttachedToVisualTree += AssociatedObject_AttachedToVisualTree;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.AttachedToVisualTree -= AssociatedObject_AttachedToVisualTree;

            _disposable?.Dispose();
        }

        private void AssociatedObject_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            var toplevel = e.Root as TopLevel;

            _disposable = toplevel.AddDisposableHandler(InputElement.PointerPressedEvent, (sender, ee) =>
            {
                if (AssociatedObject.IsVisible)
                {
                    var target = HitTarget ?? AssociatedObject;

                    var hit = target.GetVisualAt(ee.GetCurrentPoint(target).Position);

                    if (hit is null) AssociatedObject.IsVisible = false;
                }
            }, RoutingStrategies.Tunnel);
        }
    }
}