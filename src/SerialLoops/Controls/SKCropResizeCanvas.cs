using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Labs.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace SerialLoops.Controls
{
    public class SKCropResizeCanvas : SKCanvasView
    {
        private const int KEY_CHANGE_AMOUNT = 10;
        private double _aspectRatio;
        private Point? _lastPointerPosition;

        public static readonly AvaloniaProperty<SKBitmap?> SourceBitmapProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKBitmap?>(nameof(SourceBitmap));
        public static readonly AvaloniaProperty<SKBitmap?> FinalBitmapProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKBitmap?>(nameof(FinalBitmap));
        public static readonly AvaloniaProperty<SKPoint?> SelectionAreaLocationProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKPoint?>(nameof(SelectionAreaLocation));
        public static readonly AvaloniaProperty<SKPoint?> ImageLocationProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKPoint?>(nameof(ImageLocation));
        public static readonly AvaloniaProperty<double?> SourceWidthProperty = AvaloniaProperty.Register<SKCropResizeCanvas, double?>(nameof(SourceWidth));
        public static readonly AvaloniaProperty<double?> SourceHeightProperty = AvaloniaProperty.Register<SKCropResizeCanvas, double?>(nameof(SourceHeight));
        public static readonly AvaloniaProperty<float?> PreviewWidthProperty = AvaloniaProperty.Register<SKCropResizeCanvas, float?>(nameof(PreviewWidth));
        public static readonly AvaloniaProperty<float?> PreviewHeightProperty = AvaloniaProperty.Register<SKCropResizeCanvas, float?>(nameof(PreviewHeight));
        public static readonly AvaloniaProperty<bool?> PreserveAspectRatioProperty = AvaloniaProperty.Register<SKCropResizeCanvas, bool?>(nameof(PreserveAspectRatio));

        public SKBitmap? SourceBitmap
        {
            get => this.GetValue<SKBitmap?>(SourceBitmapProperty);
            set => SetValue(SourceBitmapProperty, value);
        }

        public SKBitmap? FinalBitmap
        {
            get => this.GetValue<SKBitmap?>(FinalBitmapProperty);
            set => SetValue(FinalBitmapProperty, value);
        }

        public SKPoint? SelectionAreaLocation
        {
            get => this.GetValue<SKPoint?>(SelectionAreaLocationProperty);
            set => SetValue(SelectionAreaLocationProperty, value);
        }

        public SKPoint? ImageLocation
        {
            get => this.GetValue<SKPoint?>(ImageLocationProperty);
            set => SetValue(ImageLocationProperty, value);
        }

        public double? SourceWidth
        {
            get => this.GetValue<double?>(SourceWidthProperty);
            set => SetValue(SourceWidthProperty, value);
        }

        public double? SourceHeight
        {
            get => this.GetValue<double?>(SourceHeightProperty);
            set => SetValue(SourceHeightProperty, value);
        }

        public float? PreviewWidth
        {
            get => this.GetValue<float?>(PreviewWidthProperty);
            set => SetValue(PreviewWidthProperty, value);
        }

        public float? PreviewHeight
        {
            get => this.GetValue<float?>(PreviewHeightProperty);
            set => SetValue(PreviewHeightProperty, value);
        }

        public bool? PreserveAspectRatio
        {
            get => this.GetValue<bool?>(PreserveAspectRatioProperty);
            set => SetValue(PreserveAspectRatioProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SelectionAreaLocationProperty || change.Property == ImageLocationProperty || change.Property == SourceWidthProperty || change.Property == SourceHeightProperty
                || change.Property == PreserveAspectRatioProperty)
            {
                InvalidateSurface();
                if (change.Property == PreserveAspectRatioProperty)
                {
                    if (PreserveAspectRatio ?? false)
                    {
                        _aspectRatio = (SourceHeight ?? 0) / (SourceWidth ?? 1);
                    }
                }
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            if (!IsVisible || SourceBitmap is null)
            {
                return;
            }

            OnPaintSurface(e.Surface);
            base.OnPaintSurface(e);
        }

        protected virtual void OnPaintSurface(SKSurface surface)
        {
            Scale = 1;
            SKCanvas canvas = surface.Canvas;
            canvas.Clear();
            if (SourceBitmap is not null && ImageLocation is not null && SourceWidth is not null && SourceHeight is not null)
            {
                // Draw image
                canvas.DrawBitmap(SourceBitmap,
                    new SKRect(0, 0, SourceBitmap.Width, SourceBitmap.Height),
                    new SKRect(
                        ImageLocation?.X ?? 0f,
                        ImageLocation?.Y ?? 0f,
                        (ImageLocation?.X ?? 0f) + (int)SourceWidth,
                        (ImageLocation?.Y ?? 0f) + (int)SourceHeight
                    )
                );

                // Draw Final bitmap
                SKRect areaRect = new(
                        SelectionAreaLocation?.X ?? 0f,
                        SelectionAreaLocation?.Y ?? 0f,
                        SelectionAreaLocation?.X + FinalBitmap?.Width ?? 0f,
                        SelectionAreaLocation?.Y + FinalBitmap?.Height ?? 0f
                );

                if (FinalBitmap is not null && SelectionAreaLocation is not null)
                {
                    using SKCanvas finalCanvas = new(FinalBitmap);
                    finalCanvas.Clear();
                    SKRect surfaceRect = new(
                        SelectionAreaLocation?.X ?? 0f,
                        SelectionAreaLocation?.Y ?? 0f,
                        (SelectionAreaLocation?.X ?? 0f) + (FinalBitmap?.Width ?? 0f),
                        (SelectionAreaLocation?.Y ?? 0f) + (FinalBitmap?.Height ?? 0f));
                    finalCanvas.DrawImage(surface.Snapshot(), surfaceRect, new SKRect(0, 0, FinalBitmap.Width, FinalBitmap.Height));
                    finalCanvas.Flush();
                }

                //Draw UI
                SKPath uiPath = new();
                uiPath.AddRect(new SKRect(0, 0, PreviewWidth ?? 0f, PreviewHeight ?? 0f));
                uiPath.AddRect(areaRect);
                uiPath.FillType = SKPathFillType.EvenOdd;
                this.TryFindResource("CropResizeOverlayColor", ActualThemeVariant, out object? color);
                canvas.DrawPath(uiPath, new SKPaint { Color = ((Color)color).ToSKColor() });

                canvas.Flush();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int xChange = 0;
            int yChange = 0;
            switch (e.Key)
            {
                case Key.Down:
                    yChange = KEY_CHANGE_AMOUNT;
                    break;
                case Key.Left:
                    xChange = -KEY_CHANGE_AMOUNT;
                    break;
                case Key.Right:
                    xChange = KEY_CHANGE_AMOUNT;
                    break;
                case Key.Up:
                    yChange = -KEY_CHANGE_AMOUNT;
                    break;
            }

            if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                xChange /= 10;
                yChange /= 10;
            }

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                SelectionAreaLocation?.Offset(xChange, yChange);
            }
            else
            {
                ImageLocation?.Offset(xChange, yChange);
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                return;
            }
            SourceWidth = (int)Math.Max(1, Math.Min(PreviewWidth ?? 0f, (SourceWidth ?? 0) + e.Delta.Y * 10));
            SourceHeight = (int)Math.Max(1, Math.Min(PreviewHeight ?? 0f, (SourceHeight ?? 0) + e.Delta.Y * 10 * _aspectRatio));
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            e.Handled = true;
            PointerPoint point = e.GetCurrentPoint(this);
            
            if (!point.Properties.IsLeftButtonPressed)
            {
                _lastPointerPosition = point.Position;
                return;
            }
            _lastPointerPosition ??= point.Position;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ImageLocation?.Offset((float)(point.Position.X - (_lastPointerPosition?.X ?? 0)), (float)(point.Position.Y - (_lastPointerPosition?.Y ?? 0)));
            }
            else
            {
                SelectionAreaLocation?.Offset((float)(point.Position.X - (_lastPointerPosition?.X ?? 0)), (float)(point.Position.Y - (_lastPointerPosition?.Y ?? 0)));
                if ((SelectionAreaLocation?.X ?? 0) < 0 || ((SelectionAreaLocation?.X ?? 0) > (PreviewWidth ?? 0f) - FinalBitmap?.Width))
                {
                    SelectionAreaLocation = new(Math.Max(0, Math.Min(((PreviewWidth ?? 0f) - FinalBitmap?.Width) ?? 0f, SelectionAreaLocation?.X ?? 0f)), SelectionAreaLocation?.Y ?? 0f);
                }
                if ((SelectionAreaLocation?.Y ?? 0) < 0 || ((SelectionAreaLocation?.Y ?? 0) > (PreviewHeight ?? 0f) - FinalBitmap?.Height))
                {
                    SelectionAreaLocation = new(SelectionAreaLocation?.X ?? 0f, Math.Max(0, Math.Min(((PreviewHeight ?? 0f) - FinalBitmap?.Height) ?? 0f, SelectionAreaLocation?.Y ?? 0f)));
                }
            }
            _lastPointerPosition = point.Position;
        }
    }
}
