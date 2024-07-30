using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using System.Windows.Input;

namespace SerialLoops.ViewModels.Dialogs
{
    public class ImageCropResizeDialogViewModel : ViewModelBase
    {
        private ILogger _log;
        private SKBitmap _startImage;
        private SKBitmap _finalImage;
        private double _sourceWidth;
        private double _sourceHeight;
        private float _previewWidth;
        private float _previewHeight;
        private SKPoint _imageLocation;
        private SKPoint _selectionLocation;
        private bool _preserveAspectRatio;

        public SKBitmap StartImage
        {
            get => _startImage;
            set => SetProperty(ref _startImage, value);
        }
        public SKBitmap FinalImage
        {
            get => _finalImage;
            set => SetProperty(ref _finalImage, value);
        }
        public double SourceWidth
        {
            get => _sourceWidth;
            set => SetProperty(ref _sourceWidth, value);
        }
        public double SourceHeight
        {
            get => _sourceHeight;
            set => SetProperty(ref _sourceHeight, value);
        }
        public float PreviewWidth
        {
            get => _previewWidth;
            set => SetProperty(ref _previewWidth, value);
        }
        public float PreviewHeight
        {
            get => _previewHeight;
            set => SetProperty(ref _previewHeight, value);
        }
        public SKPoint ImageLocation
        {
            get => _imageLocation;
            set => SetProperty(ref _imageLocation, value);
        }
        public SKPoint SelectionLocation
        {
            get => _selectionLocation;
            set => SetProperty(ref _selectionLocation, value);
        }
        public bool PreserveAspectRatio
        {
            get => _preserveAspectRatio;
            set => SetProperty(ref _preserveAspectRatio, value);
        }

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
