using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Models;

public class SKBitmapDrawOperation : ICustomDrawOperation
{
    public Rect Bounds { get; set; }
    public SKBitmap Bitmap { get; set; }

    public void Dispose() { }
    public bool Equals(ICustomDrawOperation other) => false;
    public bool HitTest(Point p) => Bounds.Contains(p);
    public void Render(ImmediateDrawingContext context)
    {
        if (Bitmap is { } bitmap && context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>() is { } leaseFeature)
        {
            ISkiaSharpApiLease lease = leaseFeature.Lease();
            using (lease)
            {
                lease.SkCanvas.DrawImage(SKImage.FromBitmap(bitmap),
                    SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height),
                    Extensions.HighQualitySamplingOptions,
                    new() { Color = new(255, 255, 255, (byte)(255 * lease.CurrentOpacity)) });
            }
        }
    }
}

public class SKAvaloniaImage : IImage, IDisposable
{
    private readonly SKBitmap _bitmap;
    private SKBitmapDrawOperation _drawOperation;

    public SKAvaloniaImage(SKBitmap bitmap)
    {
        _bitmap = bitmap;
        if (_bitmap?.Info.Size is { } size)
        {
            Size = new(size.Width, size.Height);
        }
    }

    public Size Size { get; }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _drawOperation?.Dispose();
        GC.SuppressFinalize(this);
    }
    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        _drawOperation ??= new()
        {
            Bitmap = _bitmap,
            Bounds = destRect,
        };
        context.Custom(_drawOperation);
    }
}
