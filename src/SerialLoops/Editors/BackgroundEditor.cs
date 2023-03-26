using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System.IO;

namespace SerialLoops.Editors
{
    public class BackgroundEditor : Editor
    {
        private BackgroundItem _bg;

        public BackgroundEditor(BackgroundItem item, ILogger log) : base(item, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _bg = (BackgroundItem)Description;
            StackLayout extrasInfo = new();
            Button exportButton = new() { Text = "Export" };
            exportButton.Click += ExportButton_Click;
            if (!string.IsNullOrEmpty(_bg.CgName))
            {
                extrasInfo.Items.Add(_bg.CgName);
                extrasInfo.Items.Add($"Unknown Extras Short: {_bg.ExtrasShort}");
                extrasInfo.Items.Add($"Unknown Extras Integer: {_bg.ExtrasInt}");
            }
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new ImageView() { Image = new SKGuiImage(_bg.GetBackground()) },
                    $"{_bg.Id} (0x{_bg.Id:X3}); {_bg.BackgroundType}",
                    exportButton,
                    extrasInfo,
                }
            };
        }

        private void ExportButton_Click(object sender, System.EventArgs e)
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filters.Add(new() { Name = "PNG Image", Extensions = new string[] { ".png" } });
            if (saveFileDialog.ShowAndReportIfFileSelected(this))
            {
                try
                {
                    using FileStream fs = File.OpenWrite(saveFileDialog.FileName);
                    _bg.GetBackground().Encode(fs, SkiaSharp.SKEncodedImageFormat.Png, 1);
                }
                catch (IOException exc)
                {
                    _log.LogError($"Failed to export background {_bg.DisplayName} to file {saveFileDialog.FileName}: {exc.Message}\n\n{exc.StackTrace}");
                }
            }
        }
    }
}
