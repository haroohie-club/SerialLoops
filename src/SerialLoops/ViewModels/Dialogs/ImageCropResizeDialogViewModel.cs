using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Skia.Helpers;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs;

public class ImageCropResizeDialogViewModel : ViewModelBase
{
    private ILogger _log;

    [Reactive]
    public Bitmap StartImage { get; set; }
    [Reactive]
    public SKBitmap FinalImage { get; set; }
    [Reactive]
    public double SourceWidth { get; set; }
    [Reactive]
    public double SourceHeight { get; set; }
    [Reactive]
    public float BoxWidth { get; set; }
    [Reactive]
    public float BoxHeight { get; set; }
    [Reactive]
    public float PreviewWidth { get; set; }
    [Reactive]
    public float PreviewHeight { get; set; }
    [Reactive]
    public SKPoint ImageLocation { get; set; }
    [Reactive]
    public SKPoint SelectionLocation { get; set; }
    [Reactive]
    public bool PreserveAspectRatio { get; set; }

    public ICommand ScaleToFitCommand { get; set; }
    public ICommand ResetPositionCommand { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public ImageCropResizeDialogViewModel(string startImagePath, int width, int height, ILogger log)
    {
        _log = log;
        StartImage = new(startImagePath);
        SourceWidth = StartImage.Size.Width;
        SourceHeight = StartImage.Size.Height;
        BoxWidth = width;
        BoxHeight = height;
        PreviewWidth = 650;
        PreviewHeight = 600;
        FinalImage = new(width, height);
        ImageLocation = new(0, 0);
        SelectionLocation = new(0, 0);
        ScaleToFitCommand = ReactiveCommand.Create<ImageCropResizeDialog>(ScaleToFit);
        ResetPositionCommand = ReactiveCommand.Create(() => ImageLocation = new(0, 0));
        SaveCommand = ReactiveCommand.CreateFromTask<ImageCropResizeDialog>(Save);
        CancelCommand = ReactiveCommand.Create<ImageCropResizeDialog>((dialog) => dialog.Close());
        PreserveAspectRatio = true;
    }

    private void ScaleToFit(ImageCropResizeDialog dialog)
    {
        double zoom;
        if (FinalImage.Width / (double)SourceWidth > FinalImage.Height / (double)SourceHeight)
        {
            zoom = FinalImage.Width / (double)SourceWidth;
        }
        else
        {
            zoom = FinalImage.Height / (double)SourceHeight;
        }
        dialog.Paz.Zoom(zoom, SourceWidth / 2, SourceHeight / 2);
        dialog.Paz.Pan(0, 0);
    }

    private async Task Save(ImageCropResizeDialog dialog)
    {
        using SKCanvas finalCanvas = new(FinalImage);
        await DrawingContextHelper.RenderAsync(finalCanvas, dialog.MainCanvas,
            new(0, 0, FinalImage.Width, FinalImage.Height), Vector.One * 300);
        finalCanvas.Flush();
        dialog.Close(FinalImage);
    }
}
