using Avalonia;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Models;

public class PathingMapIndicator : ReactiveObject
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

    public PathingMapIndicator(byte[][] walkabilityMap, int x, int y, Point gridZero, bool slgMode)
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
}
