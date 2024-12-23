using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;
using SkiaSharp;

namespace SerialLoops.ViewModels.Editors;

public class LayoutEditorViewModel : EditorViewModel
{
    private LayoutItem _layout;
    private MainWindowViewModel _mainWindow;
    public ObservableCollection<LayoutEntryWithImage> LayoutEntries { get; }

    private LayoutEntryWithImage _selectedLayoutEntry;
    public LayoutEntryWithImage SelectedLayoutEntry
    {
        get => _selectedLayoutEntry;
        set
        {
            if (_selectedLayoutEntry is not null)
            {
                _selectedLayoutEntry.IsSelected = false;
            }
            this.RaiseAndSetIfChanged(ref _selectedLayoutEntry, value);
            _selectedLayoutEntry.IsSelected = true;
        }
    }

    public ICommand ExportLayoutCommand { get; }
    public ICommand ExportSourceCommand { get; }

    public LayoutEditorViewModel(LayoutItem item, MainWindowViewModel window, ILogger log) : base(item, window, log)
    {
        _layout = item;
        _mainWindow = window;
        LayoutEntries = new(_layout.Layout.LayoutEntries.Skip(_layout.StartEntry).Take(_layout.NumEntries).Select((_, i) => new LayoutEntryWithImage(_layout, i + _layout.StartEntry)));
        ExportLayoutCommand = ReactiveCommand.CreateFromTask(ExportLayout);
        ExportSourceCommand = ReactiveCommand.CreateFromTask(ExportSource);
    }

    private async Task ExportLayout()
    {
        IStorageFile savePng = await _mainWindow.Window.ShowSaveFilePickerAsync(Strings.Export_Layout_Preview,
            [new(Strings.PNG_Image) { Patterns = ["*.png"] }], $"{_layout.DisplayName}.png");
        string path = savePng?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            await using FileStream fs = File.Create(path);
            _layout.GetLayoutImage().Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        }
    }

    private async Task ExportSource()
    {
        if (SelectedLayoutEntry is null)
        {
            return;
        }
        IStorageFile savePng = await _mainWindow.Window.ShowSaveFilePickerAsync(Strings.Export_Source_Preview,
            [new(Strings.PNG_Image) { Patterns = ["*.png"] }]);
        string path = savePng?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            await using FileStream fs = File.Create(path);
            SKBitmap preview = SelectedLayoutEntry.FullImage.Copy();
            SKCanvas canvas = new(preview);
            canvas.DrawRect(SelectedLayoutEntry.TextureX, SelectedLayoutEntry.TextureY, SelectedLayoutEntry.TextureW, SelectedLayoutEntry.TextureH, new() { Color = SKColors.Red, Style = SKPaintStyle.Stroke });
            canvas.Flush();
            preview.Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        }
    }
}
