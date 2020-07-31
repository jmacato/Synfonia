using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Synfonia.Behaviors
{
    public class StringToClassBehavior : Behavior<Control>
    {
        public static readonly DirectProperty<StringToClassBehavior, string> ClassStringProperty =
       AvaloniaProperty.RegisterDirect<StringToClassBehavior, string>(
           nameof(ClassString),
           o => o.ClassString,
           (o, v) => o.ClassString = v);

        private string _classString;

        public string ClassString
        {
            get { return _classString; }
            set { SetAndRaise(ClassStringProperty, ref _classString, value); }
        }

        private CompositeDisposable Disposables { get; set; } = new CompositeDisposable();

        protected override void OnAttached()
        {
            Disposables.Add(ClassStringProperty.Changed.AddClassHandler<StringToClassBehavior>(OnClassChanged));
            base.OnAttached();
        }

        private void OnClassChanged(StringToClassBehavior target, AvaloniaPropertyChangedEventArgs e)
        {
            target.SetClass(e.NewValue as string);
        }

        private void SetClass(string newClassString)
        {
            if (newClassString is null) return;

            AssociatedObject.Classes = new Classes(newClassString);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Classes.Clear();
            base.OnDetaching();
            Disposables?.Dispose();
        }
    }
}