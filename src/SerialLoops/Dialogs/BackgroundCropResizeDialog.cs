using Eto.Containers;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Utility;
using SkiaSharp;

namespace SerialLoops.Dialogs
{
    public class BackgroundCropResizeDialog : Dialog
    {
        private ILogger _log;
        private DragZoomImageView _imageView;
        public SKBitmap StartImage { get; set; }

        public bool SaveChanges { get; set; }
        public SKBitmap FinalImage { get; set; }

        public BackgroundCropResizeDialog(SKBitmap startImage, int width, int height, ILogger log)
        {
            _log = log;
            StartImage = startImage;

            _imageView = new() { Image = new SKGuiImage(StartImage), Width = width, Height = height };

            Content = _imageView;
            Closed += BackgroundCropResizeDialog_Closed;
        }

        private void BackgroundCropResizeDialog_Closed(object sender, System.EventArgs e)
        {
            MessageBox.Show($"{_imageView.Image.Width}, {_imageView.Image.Height}");
        }
    }
}
