using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Labs.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace SerialLoops.Controls
{
    public class SKCropResizeCanvas : SKCanvasView
    {
        private const int KEY_CHANGE_AMOUNT = 10;
        private double _aspectRatio;
        private Point? _lastPointerPosition;

        public static readonly AvaloniaProperty<SKBitmap?> SourceBitmapProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKBitmap?>(nameof(SourceBitmap));
        public static readonly AvaloniaProperty<SKBitmap?> PreviewBitmapProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKBitmap?>(nameof(PreviewBitmap));
        public static readonly AvaloniaProperty<SKBitmap?> FinalBitmapProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKBitmap?>(nameof(FinalBitmap));
        public static readonly AvaloniaProperty<SKPoint?> SelectionAreaLocationProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKPoint?>(nameof(SelectionAreaLocation));
        public static readonly AvaloniaProperty<SKPoint?> ImageLocationProperty = AvaloniaProperty.Register<SKCropResizeCanvas, SKPoint?>(nameof(ImageLocation));
        public static readonly AvaloniaProperty<double?> PreviewWidthProperty = AvaloniaProperty.Register<SKCropResizeCanvas, double?>(nameof(PreviewWidth));
        public static readonly AvaloniaProperty<double?> PreviewHeightProperty = AvaloniaProperty.Register<SKCropResizeCanvas, double?>(nameof(PreviewHeight));
        public static readonly AvaloniaProperty<bool?> PreserveAspectRatioProperty = AvaloniaProperty.Register<SKCropResizeCanvas, bool?>(nameof(PreserveAspectRatio));

        public SKBitmap? SourceBitmap
        {
            get => this.GetValue<SKBitmap?>(SourceBitmapProperty);
            set => SetValue(SourceBitmapProperty, value);
        }

        public SKBitmap? PreviewBitmap
        {
            get => this.GetValue<SKBitmap?>(PreviewBitmapProperty);
            set => SetValue(PreviewBitmapProperty, value);
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

        public double? PreviewWidth
        {
            get => this.GetValue<double?>(PreviewWidthProperty);
            set => SetValue(PreviewWidthProperty, value);
        }

        public double? PreviewHeight
        {
            get => this.GetValue<double?>(PreviewHeightProperty);
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
            if (change.Property == PreviewBitmapProperty || change.Property == SelectionAreaLocationProperty || change.Property == ImageLocationProperty || change.Property == PreviewWidthProperty || change.Property == PreviewHeightProperty
                || change.Property == PreserveAspectRatioProperty)
            {
                InvalidateSurface();
                if (change.Property == PreserveAspectRatioProperty)
                {
                    if (PreserveAspectRatio ?? false)
                    {
                        _aspectRatio = (PreviewHeight ?? 0) / (PreviewWidth ?? 1);
                    }
                }
                if (FinalBitmap is not null && SelectionAreaLocation is not null)
                {
                    using SKCanvas finalCanvas = new(FinalBitmap);
                    SKRect areaRect = new(
                            SelectionAreaLocation?.X ?? 0f,
                            SelectionAreaLocation?.Y ?? 0f,
                            (SelectionAreaLocation?.X ?? 0f) + FinalBitmap.Width,
                            (SelectionAreaLocation?.Y ?? 0f) + FinalBitmap.Height
                    );
                    finalCanvas.DrawBitmap(PreviewBitmap, areaRect, new SKRect(0, 0, FinalBitmap.Width, FinalBitmap.Height));
                    finalCanvas.Flush();
                }
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            if (!IsVisible || PreviewBitmap is null)
            {
                return;
            }

            OnPaintSurface(e.Surface.Canvas);
            base.OnPaintSurface(e);
        }

        protected virtual void OnPaintSurface(SKCanvas canvas)
        {
            canvas.Clear();
            if (SourceBitmap is not null && PreviewBitmap is not null && ImageLocation is not null && PreviewWidth is not null && PreviewHeight is not null)
            {
                // Draw image
                canvas.DrawBitmap(SourceBitmap,
                    new SKRect(0, 0, SourceBitmap.Width, SourceBitmap.Height),
                    new SKRect(
                        ImageLocation?.X ?? 0f,
                        ImageLocation?.Y ?? 0f,
                        (ImageLocation?.X ?? 0f) + (int)PreviewWidth,
                        (ImageLocation?.Y ?? 0f) + (int)PreviewHeight
                    )
                );

                // Draw UI
                SKRect areaRect = new(
                        SelectionAreaLocation?.X ?? 0f,
                        SelectionAreaLocation?.Y ?? 0f,
                        SelectionAreaLocation?.X + FinalBitmap?.Width ?? 0f,
                        SelectionAreaLocation?.Y + FinalBitmap?.Height ?? 0f
                );
                SKPath uiPath = new();
                uiPath.AddRect(new SKRect(0, 0, PreviewBitmap.Width, PreviewBitmap.Height));
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
            PreviewWidth = (int)Math.Max(1, Math.Min(PreviewBitmap.Width, (PreviewWidth ?? 0) + e.Delta.Y * 10));
            PreviewHeight = (int)Math.Max(1, Math.Min(PreviewBitmap.Height, (PreviewHeight ?? 0) + e.Delta.Y * 10 * _aspectRatio));
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
                if ((SelectionAreaLocation?.X ?? 0) < 0 || ((SelectionAreaLocation?.X ?? 0) > PreviewBitmap?.Width - FinalBitmap?.Width))
                {
                    SelectionAreaLocation = new(Math.Max(0, Math.Min((PreviewBitmap?.Width - FinalBitmap?.Width) ?? 0f, SelectionAreaLocation?.X ?? 0f)), SelectionAreaLocation?.Y ?? 0f);
                }
                if ((SelectionAreaLocation?.Y ?? 0) < 0 || ((SelectionAreaLocation?.Y ?? 0) > PreviewBitmap?.Height - FinalBitmap?.Height))
                {
                    SelectionAreaLocation = new(SelectionAreaLocation?.X ?? 0f, Math.Max(0, Math.Min((PreviewBitmap?.Height - FinalBitmap?.Height) ?? 0f, SelectionAreaLocation?.Y ?? 0f)));
                }
            }
            _lastPointerPosition = point.Position;
        }
    }
}
