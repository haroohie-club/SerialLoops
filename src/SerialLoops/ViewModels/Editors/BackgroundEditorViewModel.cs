using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SerialLoops.ViewModels.Editors
{
    public class BackgroundEditorViewModel : EditorViewModel
    {
        public BackgroundItem Bg { get; set; }
        public SKBitmap BgBitmap => Bg.GetBackground();
        public string BgDescription => $"{Bg.Id} (0x{Bg.Id:X3}); {Bg.BackgroundType}";
        public ICommand ExportCommand { get; set; }
        public ICommand ReplaceCommand { get; set; }
        public ICommand CgNameChangeCommand { get; set; }
        public bool ShowExtras => Bg.BackgroundType != BgType.TEX_BG && Bg.BackgroundType != BgType.KINETIC_SCREEN;
        public string FlagDescription => string.Format(Strings.Flag___0_, Bg.Flag);
        public string UnknownExtrasShortDescription => string.Format(Strings.Unknown_Extras_Short___0_, Bg.ExtrasShort);
        public string UnknownExtrasByteDescription => string.Format(Strings.Unknown_Extras_Byte___0_, Bg.ExtrasByte);

        public BackgroundEditorViewModel(BackgroundItem item, MainWindowViewModel window, Project project, ILogger log) : base(item, window, log, project)
        {
            Bg = item;
            ExportCommand = ReactiveCommand.CreateFromTask(ExportButton_Click);
            ReplaceCommand = ReactiveCommand.CreateFromTask(ReplaceButton_Click);
            CgNameChangeCommand = ReactiveCommand.Create<string>((cgName) =>
            {
                if (!string.IsNullOrEmpty(cgName) && !cgName.Equals(Bg.CgName))
                {
                    _project.Extra.Cgs[_project.Extra.Cgs.IndexOf(_project.Extra.Cgs.First(b => b.Name?.GetSubstitutedString(_project).Equals(Bg.CgName) ?? false))].Name = cgName.GetOriginalString(_project);
                    Bg.CgName = cgName;
                    Description.UnsavedChanges = true;
                }
            });
        }

        private async Task ExportButton_Click()
        {
            FilePickerSaveOptions saveOptions = new()
            {
                ShowOverwritePrompt = true,
                FileTypeChoices = [
                    new FilePickerFileType(Strings.PNG_Image) { Patterns = ["*.png"] }
                    ]
            };
            IStorageFile savedFile = await _window.Window.StorageProvider.SaveFilePickerAsync(saveOptions);
            if (savedFile is not null)
            {
                try
                {
                    using FileStream fs = File.Create(savedFile.Path.AbsolutePath);
                    Bg.GetBackground().Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Strings.Failed_to_export_background__0__to_file__1_, Bg.DisplayName, savedFile.Path.AbsolutePath), ex);
                }
            }
        }

        private async Task ReplaceButton_Click()
        {
            FilePickerOpenOptions openOptions = new()
            {
                AllowMultiple = false,
                SuggestedFileName = $"{Bg.Name}.png",
                FileTypeFilter = [
                    new FilePickerFileType(Strings.Supported_Images) { Patterns = ["*.bmp", "*.gif", "*.heif", "*.jpg", "*.jpeg", "*.png", "*.webp",] },
                    ]
            };
            SKBitmap original = Bg.GetBackground();
            IStorageFile openFile = (await _window.Window.StorageProvider.OpenFilePickerAsync(openOptions))?.FirstOrDefault();
            if (openFile is not null)
            {

            }
        }
    }
}
