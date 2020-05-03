using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using System;

namespace Symphony.Behaviors
{
    public class HideWhenClickedOutsideBehavior : Behavior<Visual>
    {
        private IDisposable _disposable;

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

            _disposable = toplevel.AddDisposableHandler(TopLevel.PointerPressedEvent, (sender, ee) =>
            {
                if (AssociatedObject.IsVisible)
                {
                    var hit = toplevel.InputHitTest(ee.GetCurrentPoint(null).Position);

                    if (!AssociatedObject.IsVisualAncestorOf(hit))
                    {
                        AssociatedObject.IsVisible = false;
                    }
                }
            }, RoutingStrategies.Tunnel);
        }
    }
}
