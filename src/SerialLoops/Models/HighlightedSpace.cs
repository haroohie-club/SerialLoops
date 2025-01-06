using Avalonia;
using Avalonia.Media;
using HaruhiChokuretsuLib.Archive.Data;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;

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
    [Reactive]
    public string ToolTip { get; set; }

    public HighlightedSpace(byte[][] walkabilityMap, int x, int y, Point gridZero, bool slgMode)
    {
        if (slgMode)
        {
            Point origin = new(gridZero.X - x * 32 + y * 32, gridZero.Y + x * 16 + y * 16);
            Top = origin;
            Left = new(origin.X - 32, origin.Y + 16);
            Bottom = new(origin.X, origin.Y + 32);
            Right = new(origin.X + 32, origin.Y + 16);
        }
        else
        {
            // x & y reversed from SLG mode
            (x, y) = (y, x);

            Point origin = new(gridZero.X - x * 16 + y * 16, gridZero.Y + x * 8 + y * 8);
            Top = origin;
            Left = new(origin.X - 16, origin.Y + 8);
            Bottom = new(origin.X, origin.Y + 16);
            Right = new(origin.X + 16, origin.Y + 8);
        }

        Color = walkabilityMap[x][y] switch
        {
            1 => new() { Color = new(186, 0, 128, 0) }, // walkable
            2 => new() { Color = new(186, 0, 200, 200) }, // spawnable
            _ => new() { Color = new(186, 255, 0, 0) }, // unwalkable
        };
    }

    public HighlightedSpace(InteractableObject obj, Point gridZero, Project project)
    {
        // x & y reversed from SLG mode
        Point origin = new(gridZero.X - obj.ObjectY * 16 + obj.ObjectX * 16, gridZero.Y + obj.ObjectY * 8 + obj.ObjectX * 8);
        Top = origin;
        Left = new(origin.X - 16, origin.Y + 8);
        Bottom = new(origin.X, origin.Y + 16);
        Right = new(origin.X + 16, origin.Y + 8);
        Color = new(Colors.PaleVioletRed, 0.5);
        ToolTip = obj.ObjectName.GetSubstitutedString(project);
    }

    public HighlightedSpace(UnknownMapObject2 obj, Point gridZero, bool slgMode)
    {
        if (slgMode)
        {
            Point origin = new(gridZero.X - obj.UnknownShort1 * 32 + obj.UnknownShort2 * 32, gridZero.Y + obj.UnknownShort1 * 16 + obj.UnknownShort2 * 16);
            Top = origin;
            Left = new(origin.X - 32, origin.Y + 16);
            Bottom = new(origin.X, origin.Y + 32);
            Right = new(origin.X + 32, origin.Y + 16);
        }
        else
        {
            // x & y reversed from SLG mode
            Point origin = new(gridZero.X - obj.UnknownShort2 * 16 + obj.UnknownShort1 * 16, gridZero.Y + obj.UnknownShort2 * 8 + obj.UnknownShort1 * 8);
            Top = origin;
            Left = new(origin.X - 16, origin.Y + 8);
            Bottom = new(origin.X, origin.Y + 16);
            Right = new(origin.X + 16, origin.Y + 8);
        }
        Color = new(Colors.Goldenrod, 0.5);
    }

    public HighlightedSpace(UnknownMapObject3 obj, Point gridZero, bool slgMode)
    {
        if (slgMode)
        {
            Point origin = new(gridZero.X - obj.UnknownShort1 * 32 + obj.UnknownShort2 * 32, gridZero.Y + obj.UnknownShort1 * 16 + obj.UnknownShort2 * 16);
            Top = origin;
            Left = new(origin.X - 32, origin.Y + 16);
            Bottom = new(origin.X, origin.Y + 32);
            Right = new(origin.X + 32, origin.Y + 16);
        }
        else
        {
            // x & y reversed from SLG mode
            Point origin = new(gridZero.X - obj.UnknownShort2 * 16 + obj.UnknownShort1 * 16, gridZero.Y + obj.UnknownShort2 * 8 + obj.UnknownShort1 * 8);
            Top = origin;
            Left = new(origin.X - 16, origin.Y + 8);
            Bottom = new(origin.X, origin.Y + 16);
            Right = new(origin.X + 16, origin.Y + 8);
        }
        Color = new(Colors.Fuchsia, 0.5);
    }
}
