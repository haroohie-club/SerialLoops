using Avalonia.Controls;
using Avalonia.Input;

namespace SerialLoops.Views.Dialogs;

public partial class ImageCropResizeDialog : Window
{
    public ImageCropResizeDialog()
    {
        InitializeComponent();
    }

    private void Paz_OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                Paz.PanDelta(0, -1);
                break;
            case Key.Down:
                Paz.PanDelta(0, 1);
                break;
            case Key.Left:
                Paz.PanDelta(-1, 0);
                break;
            case Key.Right:
                Paz.PanDelta(1, 0);
                break;
        }
    }
}
