using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
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

public class LayoutEntryWithImage : ReactiveObject
{
    private LayoutItem _layout;
    private int _index;
    public SKBitmap FullImage { get; }
    [Reactive]
    public SKBitmap CroppedImage { get; set; }
    [Reactive]
    public bool IsSelected { get; set; }

    private short _screenX;
    public short ScreenX
    {
        get => _screenX;
        set
        {
            this.RaiseAndSetIfChanged(ref _screenX, value);
            _layout.Layout.LayoutEntries[_index].ScreenX = _screenX;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }
    private short _screenY;
    public short ScreenY
    {
        get => _screenY;
        set
        {
            this.RaiseAndSetIfChanged(ref _screenY, value);
            _layout.Layout.LayoutEntries[_index].ScreenY = _screenY;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }
    private short _screenW;
    public short Width
    {
        get => Math.Abs(_screenW);
    }
    public short ScreenWidth
    {
        get => _screenW;
        set
        {
            this.RaiseAndSetIfChanged(ref _screenW, value);
            this.RaisePropertyChanged(nameof(Width));
            _layout.Layout.LayoutEntries[_index].ScreenW = _screenW;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }
    private short _screenH;
    public short Height
    {
        get => Math.Abs(_screenH);
    }
    public short ScreenHeight
    {
        get => _screenH;
        set
        {
            this.RaiseAndSetIfChanged(ref _screenH, value);
            this.RaisePropertyChanged(nameof(Height));
            _layout.Layout.LayoutEntries[_index].ScreenH = _screenH;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }

    private short _textureX;
    public short TextureX
    {
        get => _textureX;
        set
        {
            this.RaiseAndSetIfChanged(ref _textureX, value);
            _layout.Layout.LayoutEntries[_index].TextureX = _textureX;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }
    private short _textureY;
    public short TextureY
    {
        get => _textureY;
        set
        {
            this.RaiseAndSetIfChanged(ref _textureY, value);
            _layout.Layout.LayoutEntries[_index].TextureY = _textureY;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }
    private short _textureW;
    public short TextureW
    {
        get => _textureW;
        set
        {
            this.RaiseAndSetIfChanged(ref _textureW, value);
            _layout.Layout.LayoutEntries[_index].TextureW = _textureW;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }
    private short _textureH;
    public short TextureH
    {
        get => _textureH;
        set
        {
            this.RaiseAndSetIfChanged(ref _textureH, value);
            _layout.Layout.LayoutEntries[_index].TextureH = _textureH;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }

    private SKColor _tint;
    public SKColor Tint
    {
        get => _tint;
        set
        {
            this.RaiseAndSetIfChanged(ref _tint, value);
            _layout.Layout.LayoutEntries[_index].Tint = _tint;
            CroppedImage = _layout.GetLayoutEntryRender(_index);
            _layout.UnsavedChanges = true;
        }
    }

    public LayoutEntryWithImage(LayoutItem layout, int idx)
    {
        _layout = layout;
        _index = idx;
        FullImage = _layout.Layout.LayoutEntries[_index].RelativeShtxIndex >= 0 ? _layout.TilesDict[_layout.Layout.LayoutEntries[_index].RelativeShtxIndex] : null;
        CroppedImage = _layout.GetLayoutEntryRender(_index);
        _textureX = _layout.Layout.LayoutEntries[_index].TextureX;
        _textureY = _layout.Layout.LayoutEntries[_index].TextureY;
        _textureW = _layout.Layout.LayoutEntries[_index].TextureW;
        _textureH = _layout.Layout.LayoutEntries[_index].TextureH;
        _screenW = _layout.Layout.LayoutEntries[_index].ScreenW;
        _screenH = _layout.Layout.LayoutEntries[_index].ScreenH;
        _screenX = _layout.Layout.LayoutEntries[_index].ScreenX;
        _screenY = _layout.Layout.LayoutEntries[_index].ScreenY;
        _tint = _layout.Layout.LayoutEntries[_index].Tint;
    }
}
