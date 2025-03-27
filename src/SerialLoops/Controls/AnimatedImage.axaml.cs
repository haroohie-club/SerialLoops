using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace SerialLoops.Controls;

[PseudoClasses(":animating")]
public partial class AnimatedImage : UserControl
{
    public AnimatedImage()
    {
        InitializeComponent();
    }

    public void Animate()
    {
        PseudoClasses.Set(":animating", false);
        PseudoClasses.Set(":animating", true);
    }
}
