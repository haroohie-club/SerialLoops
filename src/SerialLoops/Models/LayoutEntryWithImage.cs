using System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.Models;

public class LayoutEntryWithImage : ReactiveObject
{
    private LayoutItem _layout;
    public int Index { get; }
    public SKBitmap FullImage { get; }
    [Reactive]
    public SKBitmap CroppedImage { get; set; }
    [Reactive]
    public bool IsSelected { get; set; }

    [Reactive]
    public bool HitTestVisible { get; set; } = true;

    private short _screenX;
    public short ScreenX
    {
        get => _screenX;
        set
        {
            this.RaiseAndSetIfChanged(ref _screenX, value);
            if (_layout is null)
            {
                return;
            }
            _layout.Layout.LayoutEntries[Index].ScreenX = _screenX;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            if (_layout is null)
            {
                return;
            }
            _layout.Layout.LayoutEntries[Index].ScreenY = _screenY;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            if (_layout is null)
            {
                return;
            }
            _layout.Layout.LayoutEntries[Index].ScreenW = _screenW;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            if (_layout is null)
            {
                return;
            }
            _layout.Layout.LayoutEntries[Index].ScreenH = _screenH;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            _layout.Layout.LayoutEntries[Index].TextureX = _textureX;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            _layout.Layout.LayoutEntries[Index].TextureY = _textureY;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            _layout.Layout.LayoutEntries[Index].TextureW = _textureW;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            _layout.Layout.LayoutEntries[Index].TextureH = _textureH;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
            _layout.Layout.LayoutEntries[Index].Tint = _tint;
            CroppedImage = _layout.GetLayoutEntryRender(Index);
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
        Index = idx;
        FullImage = _layout.Layout.LayoutEntries[Index].RelativeShtxIndex >= 0 ? _layout.TilesDict[_layout.Layout.LayoutEntries[Index].RelativeShtxIndex] : null;
        CroppedImage = _layout.GetLayoutEntryRender(Index);
        _textureX = _layout.Layout.LayoutEntries[Index].TextureX;
        _textureY = _layout.Layout.LayoutEntries[Index].TextureY;
        _textureW = _layout.Layout.LayoutEntries[Index].TextureW;
        _textureH = _layout.Layout.LayoutEntries[Index].TextureH;
        _screenW = _layout.Layout.LayoutEntries[Index].ScreenW;
        _screenH = _layout.Layout.LayoutEntries[Index].ScreenH;
        _screenX = _layout.Layout.LayoutEntries[Index].ScreenX;
        _screenY = _layout.Layout.LayoutEntries[Index].ScreenY;
        _tint = _layout.Layout.LayoutEntries[Index].Tint;
    }

    public LayoutEntryWithImage()
    {
    }
}
