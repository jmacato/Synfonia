using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Synfonia.Behaviors
{
    public class CommandOnDoubleClickBehavior : CommandBasedBehavior<Control>
    {
        private CompositeDisposable Disposables { get; set; }

        protected override void OnAttached()
        {
            Disposables = new CompositeDisposable();

            base.OnAttached();

            Disposables.Add(AssociatedObject.AddDisposableHandler(InputElement.DoubleTappedEvent,
                (sender, e) => e.Handled = ExecuteCommand()));
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            Disposables?.Dispose();
        }
    }
}