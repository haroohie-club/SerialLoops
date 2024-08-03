using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

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
            IStorageFile savedFile = await _window.Window.ShowSaveFilePickerAsync(Strings.Export_Background_Image, [new FilePickerFileType(Strings.PNG_Image) { Patterns = ["*.png"] }], $"{Bg.Name}.png");
            if (savedFile is not null)
            {
                try
                {
                    using FileStream fs = File.Create(savedFile.Path.LocalPath);
                    Bg.GetBackground().Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Strings.Failed_to_export_background__0__to_file__1_, Bg.DisplayName, savedFile.Path.LocalPath), ex);
                }
            }
        }

        private async Task ReplaceButton_Click()
        {
            IStorageFile openFile = await _window.Window.ShowOpenFilePickerAsync(Strings.Replace_Background_Image, [new FilePickerFileType(Strings.Supported_Images) { Patterns = Shared.SupportedImageFiletypes }]);
            if (openFile is not null)
            {
                SKBitmap original = Bg.GetBackground();
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
                        await new ProgressDialog(() => Bg.SetBackground(finalImage, tracker, _log),
                            () => { }, tracker, string.Format(Strings.Replacing__0____, Bg.DisplayName)).ShowDialog(_window.Window);
                        this.RaisePropertyChanged(nameof(BgBitmap));
                        Description.UnsavedChanges = true;
                    }
                    catch (Exception ex)
                    {
                        _log.LogException(string.Format(Strings.Failed_to_replace_background__0__with_file__1_, Bg.DisplayName, openFile.Path.LocalPath), ex);
                    }
                }
            }
        }
    }
}
