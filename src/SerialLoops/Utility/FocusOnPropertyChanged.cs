using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace SerialLoops.Utility;

public class FocusOnAttachedToVisualTree : Behavior<Control>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.AttachedToVisualTree += ApplyFocus;
    }
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.AttachedToVisualTree -= ApplyFocus;
    }

    private void ApplyFocus(object sender, VisualTreeAttachmentEventArgs e)
    {
        if (!AssociatedObject.IsFocused) AssociatedObject.Focus();
    }

}
