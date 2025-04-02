using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Shapes;

namespace SerialLoops.Controls;

[PseudoClasses(":animating")]
public class AnimatingRectangle : Rectangle
{
    public void Animate()
    {
        PseudoClasses.Set(":animating", false);
        PseudoClasses.Set(":animating", true);
    }
}
