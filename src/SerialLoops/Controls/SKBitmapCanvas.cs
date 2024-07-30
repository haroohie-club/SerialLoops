using Avalonia;
using Avalonia.Labs.Controls;
using SkiaSharp;

namespace SerialLoops.Controls
{
    public class SKBitmapCanvas : SKCanvasView
    {
        public static readonly AvaloniaProperty<SKBitmap?> SourceProperty = AvaloniaProperty.Register<SKBitmapCanvas, SKBitmap?>(nameof(Source));

        public SKBitmap? Source
        {
            get => this.GetValue<SKBitmap?>(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SourceProperty)
            {
                InvalidateSurface();
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            if (!IsVisible || Source is null)
            {
                return;
            }

            OnPaintSurface(e.Surface.Canvas, Source);
            base.OnPaintSurface(e);
        }

        protected virtual void OnPaintSurface(SKCanvas canvas, SKBitmap source)
        {
            canvas.Clear();
            if (source is not null)
            {
                canvas.DrawBitmap(source, SKRect.Create(source.Width, source.Height));
            }
        }
    }
}
