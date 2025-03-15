using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using HaruhiChokuretsuLib.Archive.Data;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SkiaSharp;

namespace SerialLoops.Models;

public class HighlightedSpace : ReactiveObject
{
    [Reactive]
    public Point Top { get; set; }
    [Reactive]
    public Point Left { get; set; }
    [Reactive]
    public Point Right { get; set; }
    [Reactive]
    public Point Bottom { get; set; }
    [Reactive]
    public SolidColorBrush Color { get; set; }

    public HighlightedSpace(byte[][] walkabilityMap, int x, int y, Point gridZero, bool slgMode)
    {
        SetupBounds(MapItem.GetPositionFromGrid(x, y, gridZero.ToSKPoint(), slgMode), slgMode);
        if (!slgMode)
        {
            // x & y reversed from SLG mode
            (x, y) = (y, x);
        }

        Color = walkabilityMap[x][y] switch
        {
            1 => new() { Color = new(186, 0, 128, 0) }, // walkable
            2 => new() { Color = new(186, 0, 200, 200) }, // spawnable
            _ => new() { Color = new(186, 255, 0, 0) }, // unwalkable
        };
    }

    public HighlightedSpace(InteractableObject obj, Point gridZero)
    {
        // x & y reversed from SLG mode
        Point origin = new(gridZero.X - obj.ObjectY * 16 + obj.ObjectX * 16, gridZero.Y + obj.ObjectY * 8 + obj.ObjectX * 8);
        Top = origin;
        Left = new(origin.X - 16, origin.Y + 8);
        Bottom = new(origin.X, origin.Y + 16);
        Right = new(origin.X + 16, origin.Y + 8);
        Color = new(Colors.PaleVioletRed, 0.5);
    }

    public HighlightedSpace(UnknownMapObject2 obj, Point gridZero, bool slgMode)
    {
        SetupBounds(MapItem.GetPositionFromGrid(obj.UnknownShort1, obj.UnknownShort2, gridZero.ToSKPoint(), slgMode), slgMode);
        Color = new(Colors.Goldenrod, 0.5);
    }

    public HighlightedSpace(ObjectMarker obj, Point gridZero, bool slgMode)
    {
        SetupBounds(MapItem.GetPositionFromGrid(obj.ObjectX, obj.ObjectY, gridZero.ToSKPoint(), slgMode), slgMode);
        Color = new(Colors.Fuchsia, 0.5);
    }

    private void SetupBounds(SKPoint skOrigin, bool slgMode)
    {
        Point origin = new(skOrigin.X, skOrigin.Y);
        Top = origin;
        if (slgMode)
        {
            Left = new(origin.X - 32, origin.Y + 16);
            Bottom = new(origin.X, origin.Y + 32);
            Right = new(origin.X + 32, origin.Y + 16);
        }
        else
        {
            Left = new(origin.X - 16, origin.Y + 8);
            Bottom = new(origin.X, origin.Y + 16);
            Right = new(origin.X + 16, origin.Y + 8);
        }
    }
}
