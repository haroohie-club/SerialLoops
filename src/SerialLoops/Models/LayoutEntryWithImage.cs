using System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.Models;

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

    // Used for maps to filter within layers
    [Reactive]
    public int Layer { get; set; }
    [Reactive]
    public bool IsVisible { get; set; } = true;

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
