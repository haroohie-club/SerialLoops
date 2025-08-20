using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using ReactiveHistory;
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
            if (_selectedLayoutEntry is not null)
            {
                _selectedLayoutEntry.IsSelected = true;
            }
        }
    }

    private StackHistory _history;

    public ICommand ExportLayoutCommand { get; }
    public ICommand ExportSourceCommand { get; }

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public KeyGesture UndoGesture { get; }
    public KeyGesture RedoGesture { get; }

    public LayoutEditorViewModel(LayoutItem item, MainWindowViewModel window, ILogger log) : base(item, window, log)
    {
        _history = new();

        _layout = item;
        _mainWindow = window;
        LayoutEntries = new(_layout.Layout.LayoutEntries.Skip(_layout.StartEntry).Take(_layout.NumEntries).Select((_, i) => new LayoutEntryWithImage(_layout, i + _layout.StartEntry, history: _history)));
        ExportLayoutCommand = ReactiveCommand.CreateFromTask(ExportLayout);
        ExportSourceCommand = ReactiveCommand.CreateFromTask(ExportSource);

        UndoCommand = ReactiveCommand.Create(() => _history.Undo());
        RedoCommand = ReactiveCommand.Create(() => _history.Redo());
        UndoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Z);
        RedoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Y);
    }

    private async Task ExportLayout()
    {
        IStorageFile savePng = await _mainWindow.Window.ShowSaveFilePickerAsync(Strings.LayoutEditorExportPreviewButton,
            [new(Strings.FiletypePng) { Patterns = ["*.png"] }], $"{_layout.DisplayName}.png");
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
        IStorageFile savePng = await _mainWindow.Window.ShowSaveFilePickerAsync(Strings.LayoutEditorExportSourcePreviewButton,
            [new(Strings.FiletypePng) { Patterns = ["*.png"] }]);
        string path = savePng?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            await using FileStream fs = File.Create(path);
            SKBitmap preview = SelectedLayoutEntry.FullImage.Copy();
            using SKCanvas canvas = new(preview);
            canvas.DrawRect(SelectedLayoutEntry.TextureX, SelectedLayoutEntry.TextureY, SelectedLayoutEntry.TextureW, SelectedLayoutEntry.TextureH, new() { Color = SKColors.Red, Style = SKPaintStyle.Stroke });
            canvas.Flush();
            preview.Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
        }
    }
}
