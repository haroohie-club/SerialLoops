using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors
{
    public class SystemTextureEditorViewModel : EditorViewModel
    {
        public SystemTextureItem SystemTexture { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand ReplaceCommand { get; set; }
        public ICommand ReplaceWithPaletteCommand { get; set; }
        public SKBitmap SystemTextureBitmap => SystemTexture.GetTexture();
        public bool UsesCommonPalette => SystemTexture.UsesCommonPalette();
        public SKBitmap PaletteBitmap => SystemTexture.Grp.GetPalette();

        public SystemTextureEditorViewModel(SystemTextureItem item, MainWindowViewModel window, Project project, ILogger log) : base(item, window, log, project)
        {
            SystemTexture = item;
            ExportCommand = ReactiveCommand.CreateFromTask(ExportButton_Click);
            ReplaceCommand = ReactiveCommand.CreateFromTask(ReplaceButton_Click);
            ReplaceWithPaletteCommand = ReactiveCommand.CreateFromTask(ReplaceWithPaletteButton_Click);
        }

        private async Task ExportButton_Click()
        {
            IStorageFile savedFile = await _window.Window.ShowSaveFilePickerAsync(Strings.Export_System_Texture, [new FilePickerFileType(Strings.PNG_Image) { Patterns = ["*.png"] }], $"{SystemTexture.Grp.Index:D4}.png");
            if (savedFile is not null)
            {
                try
                {
                    using FileStream fs = File.Create(savedFile.Path.LocalPath);
                    SystemTexture.GetTexture().Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Strings.Failed_to_export_system_texture__0__to_file__1_, SystemTexture.DisplayName, savedFile.Path.LocalPath), ex);
                }
            }
        }

        private async Task ReplaceButton_Click()
        {
            await ReplaceImage(false);
        }

        private async Task ReplaceWithPaletteButton_Click()
        {
            await ReplaceImage(true);
        }

        private async Task ReplaceImage(bool replacePalette)
        {
            SKBitmap original = SystemTexture.GetTexture();
            IStorageFile openFile = await _window.Window.ShowOpenFilePickerAsync(Strings.Replace_System_Texture, [new FilePickerFileType(Strings.Supported_Images) { Patterns = Shared.SupportedImageFiletypes }]);
            if (openFile is not null)
            {
                SKBitmap newImage = SKBitmap.Decode(openFile.Path.LocalPath);
                ImageCropResizeDialogViewModel cropResizeDialogViewModel = new(newImage, original.Width, original.Height, _log);
                SKBitmap finalImage = await new ImageCropResizeDialog()
                {
                    DataContext = cropResizeDialogViewModel,
                }.ShowDialog<SKBitmap>(_window.Window);
                if (finalImage is not null)
                {
                    try
                    {
                        LoopyProgressTracker tracker = new();
                        await new ProgressDialog(() => SystemTexture.SetTexture(finalImage, replacePalette, _log),
                            () => { }, tracker, string.Format(Strings.Replacing__0____, SystemTexture.DisplayName)).ShowDialog(_window.Window);
                        this.RaisePropertyChanged(nameof(SystemTextureBitmap));
                        Description.UnsavedChanges = true;
                    }
                    catch (Exception ex)
                    {
                        _log.LogException(string.Format(Strings.Failed_to_replace_system_texture__0__with_file__1_, SystemTexture.DisplayName, openFile.Path.LocalPath), ex);
                    }
                }
            }
        }
    }
}
