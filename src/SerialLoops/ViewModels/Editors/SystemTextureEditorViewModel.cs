using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

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
            FilePickerSaveOptions saveOptions = new()
            {
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    new FilePickerFileType(Strings.PNG_Image) { Patterns = ["*.png"] }
                ]
            };
            IStorageFile savedFile = await _window.Window.StorageProvider.SaveFilePickerAsync(saveOptions);
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

        private async Task ReplaceImage(bool ReplacePalette)
        {
            FilePickerOpenOptions openOptions = new()
            {
                AllowMultiple = false,
                SuggestedFileName = $"{SystemTexture.Name}.png",
                FileTypeFilter =
                [
                    new FilePickerFileType(Strings.Supported_Images) { Patterns = ["*.bmp", "*.gif", "*.heif", "*.jpg", "*.jpeg", "*.png", "*.webp"] },
                ]
            };
            SKBitmap original = SystemTexture.GetTexture();
            IStorageFile openFile = (await _window.Window.StorageProvider.OpenFilePickerAsync(openOptions))?.FirstOrDefault();
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
                        await new ProgressDialog(() => SystemTexture.SetTexture(finalImage, ReplacePalette, _log),
                            () => { }, tracker, string.Format(Strings.Replacing__0____, SystemTexture.DisplayName)).ShowDialog(_window.Window);
                        OnPropertyChanged(nameof(SystemTextureBitmap));
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
