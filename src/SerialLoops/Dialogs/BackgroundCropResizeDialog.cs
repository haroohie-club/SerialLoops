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
        public SKBitmap StartImage { get; set; }

        public bool SaveChanges { get; set; }
        public SKBitmap FinalImage { get; set; }

        public BackgroundCropResizeDialog(SKBitmap startImage, int width, int height, ILogger log)
        {
            _log = log;
            StartImage = startImage;

            FinalImage = new(width, height);
            SKCanvas finalCanvas = new(FinalImage);

            finalCanvas.DrawBitmap(StartImage, new SKRect(0, 0, StartImage.Width, StartImage.Height), new SKRect(0, 0, FinalImage.Width, FinalImage.Height));

            ImageView imageView = new() { Image = new SKGuiImage(FinalImage), Width = width, Height = height };

            Button saveButton = new() { Text = "Save" };
            Button cancelButton = new() { Text = "Cancel" };
            saveButton.Click += (sender, args) =>
            {
                SaveChanges = true;
                Close();
            };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    imageView,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 3,
                        Items =
                        {
                            saveButton,
                            cancelButton,
                        }
                    }
                }
            };
        }
    }
}
