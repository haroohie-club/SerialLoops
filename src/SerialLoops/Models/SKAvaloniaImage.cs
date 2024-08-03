using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace SerialLoops.Models
{
    public class SKBitmapDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }
        public SKBitmap Bitmap { get; set; }

        public void Dispose() { }
        public bool Equals(ICustomDrawOperation other) => false;
        public bool HitTest(Point p) => Bounds.Contains(p);
        public void Render(ImmediateDrawingContext context)
        {
            if (Bitmap is SKBitmap bitmap && context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>() is ISkiaSharpApiLeaseFeature leaseFeature)
            {
                ISkiaSharpApiLease lease = leaseFeature.Lease();
                using (lease)
                {
                    lease.SkCanvas.DrawBitmap(bitmap, SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height), new()
                    {
                        FilterQuality = SKFilterQuality.High,
                    });
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
            if (_bitmap?.Info.Size is SKSizeI size)
            {
                Size = new(size.Width, size.Height);
            }
        }

        public Size Size { get; }

        public void Dispose() => throw new NotImplementedException();
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            if (_drawOperation is null)
            {
                _drawOperation = new()
                {
                    Bitmap = _bitmap,
                    Bounds = sourceRect,
                };
                context.Custom(_drawOperation);
            }
        }
    }
}
