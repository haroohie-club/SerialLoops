using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace SerialLoops.Controls;

[PseudoClasses(":animating")]
public class AnimatingImage : Image
{
    public void Animate()
    {
        PseudoClasses.Set(":animating", false);
        PseudoClasses.Set(":animating", true);
    }
}
