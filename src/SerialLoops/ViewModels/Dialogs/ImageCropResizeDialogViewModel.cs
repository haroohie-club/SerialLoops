using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs
{
    public class ImageCropResizeDialogViewModel : ViewModelBase
    {
        private ILogger _log;

        [Reactive]
        public SKBitmap StartImage { get; set; }
        [Reactive]
        public SKBitmap FinalImage { get; set; }
        [Reactive]
        public double SourceWidth { get; set; }
        [Reactive]
        public double SourceHeight { get; set; }
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

        public ImageCropResizeDialogViewModel(SKBitmap startImage, int width, int height, ILogger log)
        {
            _log = log;
            StartImage = startImage;
            SourceWidth = startImage.Width;
            SourceHeight = startImage.Height;
            PreviewWidth = 650;
            PreviewHeight = 600;
            FinalImage = new(width, height);
            ImageLocation = new(0, 0);
            SelectionLocation = new(0, 0);
            ScaleToFitCommand = ReactiveCommand.Create(() =>
            {
                SourceWidth = FinalImage.Width;
                SourceHeight = FinalImage.Height;
            });
            ResetPositionCommand = ReactiveCommand.Create(() => ImageLocation = new(0, 0));
            SaveCommand = ReactiveCommand.Create<ImageCropResizeDialog>((dialog) => dialog.Close(FinalImage));
            CancelCommand = ReactiveCommand.Create<ImageCropResizeDialog>((dialog) => dialog.Close());
            PreserveAspectRatio = true;
        }
    }
}
