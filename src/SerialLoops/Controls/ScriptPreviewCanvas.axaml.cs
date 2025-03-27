using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Controls;

namespace SerialLoops.Controls;

public partial class ScriptPreviewCanvas : UserControl
{
    public ScriptPreviewCanvas()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ((ScriptPreviewCanvasViewModel)DataContext).PreviewCanvas = this;
    }

    public void RunNonLoopingAnimations()
    {
        ChibiEmote.Animate();
        Item.Animate();
    }
}

