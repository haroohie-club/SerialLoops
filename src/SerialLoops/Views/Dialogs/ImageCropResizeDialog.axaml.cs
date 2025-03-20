using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SerialLoops.Views.Dialogs;

public partial class ImageCropResizeDialog : Window
{
    public ImageCropResizeDialog()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Paz.Focus();
    }

    private void Paz_OnKeyDown(object sender, KeyEventArgs e)
    {
        double panDelta = e.KeyModifiers == KeyModifiers.Shift ? 10 : 1;
        double zoomDelta = e.KeyModifiers == KeyModifiers.Shift ? 0.1 : 0.01;
        switch (e.Key)
        {
            case Key.Up:
                Paz.PanDelta(0, -panDelta);
                break;
            case Key.Down:
                Paz.PanDelta(0, panDelta);
                break;
            case Key.Left:
                Paz.PanDelta(-panDelta, 0);
                break;
            case Key.Right:
                Paz.PanDelta(panDelta, 0);
                break;
            case Key.OemPlus:
                Paz.ZoomDeltaTo(zoomDelta, 0, 0);
                break;
            case Key.OemMinus:
                Paz.ZoomDeltaTo(-zoomDelta, 0, 0);
                break;
        }
    }
}
