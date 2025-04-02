using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace SerialLoops.Controls;

/// <summary>
/// Different from an AnimatedImage which contains a frame animation, this image is capable of one-time control animations
/// AnimatedImage can also do this stuff tho :P
/// </summary>
[PseudoClasses(":animating")]
public class AnimatingImage : Image
{
    public void Animate()
    {
        PseudoClasses.Set(":animating", false);
        PseudoClasses.Set(":animating", true);
    }
}
