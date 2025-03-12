using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class ItemEditorViewModel : EditorViewModel
{
    private ItemItem _item;
    public SKBitmap ItemBitmap => _item.GetImage();

    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }

    public ItemEditorViewModel(ItemItem item, MainWindowViewModel window, ILogger log) : base(item, window, log)
    {
        _item = item;
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        ImportCommand = ReactiveCommand.CreateFromTask(Import);
    }

    private async Task Export()
    {
        string exportPath = (await Window.Window.ShowSaveFilePickerAsync(Strings.Export_Item_Image,
            [new(Strings.PNG_Image) { Patterns = ["*.png"] }]))?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(exportPath))
        {
            await using FileStream fs = File.Create(exportPath);
            ItemBitmap.Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        }
    }

    private async Task Import()
    {
        string importPath = (await Window.Window.ShowOpenFilePickerAsync(Strings.Import_Item_Image,
            [new(Strings.Supported_Images) { Patterns = Shared.SupportedImageFiletypes }]))?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(importPath))
        {
            SKBitmap original = _item.GetImage();
            ImageCropResizeDialogViewModel cropResizeDialogViewModel = new(importPath, original.Width, original.Height, _log);
            SKBitmap finalImage = await new ImageCropResizeDialog
            {
                DataContext = cropResizeDialogViewModel,
            }.ShowDialog<SKBitmap>(Window.Window);
            if (finalImage is not null)
            {
                try
                {
                    ProgressDialogViewModel tracker = new(string.Format(Strings.Replacing__0____, _item.DisplayName));
                    tracker.InitializeTasks(() => _item.SetImage(finalImage, tracker, _log),
                        () => { });
                    await new ProgressDialog { DataContext = tracker }.ShowDialog(Window.Window);
                    this.RaisePropertyChanged(nameof(ItemBitmap));
                    Description.UnsavedChanges = true;
                }
                catch (Exception ex)
                {
                    _log.LogException(string.Format(Strings.Failed_to_replace_background__0__with_file__1_, _item.DisplayName, importPath), ex);
                }
            }
        }
    }
}
