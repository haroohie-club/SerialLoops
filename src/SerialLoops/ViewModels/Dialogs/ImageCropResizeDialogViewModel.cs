using HaruhiChokuretsuLib.Util;
using SkiaSharp;

namespace SerialLoops.ViewModels.Dialogs
{
    public class ImageCropResizeDialogViewModel : ViewModelBase
    {
        private ILogger _log;

        public SKBitmap StartImage { get; set; }
        public SKBitmap FinalImage { get; set; }
        public bool SaveChanges { get; set; }
    }
}
