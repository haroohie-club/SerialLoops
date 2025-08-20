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
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class BackgroundEditorViewModel : EditorViewModel
{
    public BackgroundItem Bg { get; set; }
    public SKBitmap BgBitmap => Bg.GetBackground();
    public string BgDescription => $"{Bg.Id} (0x{Bg.Id:X3}); {Bg.BackgroundType}";

    public string DescriptionToolTip => Bg.BackgroundType switch
    {
        BgType.KINETIC_SCREEN => Strings.BackgroundEditorHelpBgTypeKINETIC_SCREEN,
        BgType.TEX_BG => Strings.BackgroundEditorHelpBgTypeTEX_BG,
        BgType.TEX_CG => Strings.BackgroundEditorHelpBgTypeTEX_CG,
        BgType.TEX_CG_DUAL_SCREEN => Strings.BackgroundEditorHelpBgTypeTEX_CG_DUAL_SCREEN,
        BgType.TEX_CG_WIDE => Strings.BackgroundEditorHelpBgTypeTEX_CG_WIDE,
        BgType.TEX_CG_SINGLE => Strings.BackgroundEditorHelpBgTypeTEX_CG_SINGLE,
        _ => string.Empty,
    };
    public ICommand ExportCommand { get; set; }
    public ICommand ReplaceCommand { get; set; }
    public ICommand CgNameChangeCommand { get; set; }
    public bool ShowExtras => Bg.BackgroundType != BgType.TEX_BG && Bg.BackgroundType != BgType.KINETIC_SCREEN;
    public string FlagDescription => string.Format(Strings.EditorFlagIdLabel, Bg.Flag - 1);
    public string UnknownExtrasShortDescription => string.Format(Strings.BgEditorUnknownExtrasShort, Bg.ExtrasShort);
    public string UnknownExtrasByteDescription => string.Format(Strings.BgEditorUnknownExtrasByte, Bg.ExtrasByte);

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
                new(Strings.FiletypePng) { Patterns = ["*.png"] }
            ]
        };
        IStorageFile savedFile = await Window.Window.ShowSaveFilePickerAsync(Strings.BackgroundEditorExportFileDialogTitle, [new(Strings.FiletypePng) { Patterns = ["*.png"] }], $"{Bg.Name}.png");
        if (savedFile is not null)
        {
            try
            {
                using FileStream fs = File.Create(savedFile.Path.LocalPath);
                Bg.GetBackground().Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            }
            catch (Exception ex)
            {
                _log.LogException(string.Format(Strings.ErrorFailedExportingBackground, Bg.DisplayName, savedFile.Path.LocalPath), ex);
            }
        }
    }

    private async Task ReplaceButton_Click()
    {
        IStorageFile openFile = await Window.Window.ShowOpenFilePickerAsync(Strings.BackgroundEditorReplaceBackgroundImage, [new(Strings.FiletypeSupportedImages) { Patterns = Shared.SupportedImageFiletypes }]);
        if (openFile is not null)
        {
            SKBitmap original = Bg.GetBackground();
            ImageCropResizeDialogViewModel cropResizeDialogViewModel = new(openFile.TryGetLocalPath(), original.Width, original.Height, _log);
            SKBitmap finalImage = await new ImageCropResizeDialog
            {
                DataContext = cropResizeDialogViewModel,
            }.ShowDialog<SKBitmap>(Window.Window);
            if (finalImage is not null)
            {
                try
                {
                    ProgressDialogViewModel tracker = new(string.Format(Strings.ReplacingItemProgressMessage, Bg.DisplayName));
                    bool success = false;
                    tracker.InitializeTasks(
                        () => success = Bg.SetBackground(finalImage, tracker, _log, _project.Localize),
                        () => { });
                    await new ProgressDialog { DataContext = tracker }.ShowDialog(Window.Window);
                    if (success)
                    {
                        this.RaisePropertyChanged(nameof(BgBitmap));
                        Description.UnsavedChanges = true;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Strings.ErrorFailedReplacingBackground, Bg.DisplayName, openFile.Path.LocalPath), ex);
                }
            }
        }
    }
}
