using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SkiaSharp;
using System.Windows.Input;

namespace SerialLoops.ViewModels.Dialogs
{
    public class ImageCropResizeDialogViewModel : ViewModelBase
    {
        private ILogger _log;

        private SKBitmap _startImage;
        private SKBitmap _previewImage;
        private SKBitmap _finalImage;
        private double _previewWidth;
        private double _previewHeight;
        private SKPoint _imageLocation;
        private SKPoint _selectionLocation;
        private bool _saveChanges;
        private bool _preserveAspectRatio;

        public SKBitmap StartImage
        {
            get => _startImage;
            set => SetProperty(ref _startImage, value);
        }
        public SKBitmap PreviewImage
        {
            get => _previewImage;
            set => SetProperty(ref _previewImage, value);
        }
        public SKBitmap FinalImage
        {
            get => _finalImage;
            set => SetProperty(ref _finalImage, value);
        }
        public double PreviewWidth
        {
            get => _previewWidth;
            set => SetProperty(ref _previewWidth, value);
        }
        public double PreviewHeight
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
        public bool SaveChanges
        {
            get => _saveChanges;
            set => SetProperty(ref _saveChanges, value);
        }
        public bool PreserveAspectRatio
        {
            get => _preserveAspectRatio;
            set => SetProperty(ref _preserveAspectRatio, value);
        }

        public ICommand ScaleToFitCommand { get; set; }

        public ImageCropResizeDialogViewModel(SKBitmap startImage, int width, int height, ILogger log)
        {
            _log = log;
            StartImage = startImage;
            PreviewImage = new(650, 600);
            PreviewWidth = startImage.Width;
            PreviewHeight = startImage.Height;
            FinalImage = new(width, height);
            ImageLocation = new(0, 0);
            SelectionLocation = new(0, 0);
            ScaleToFitCommand = ReactiveCommand.Create(() =>
            {
                PreviewWidth = FinalImage.Width;
                PreviewHeight = FinalImage.Height;
            });
            PreserveAspectRatio = true;
        }
    }
}
