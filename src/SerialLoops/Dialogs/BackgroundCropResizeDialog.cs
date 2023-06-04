using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Utility;
using SkiaSharp;
using System;

namespace SerialLoops.Dialogs
{
    public class BackgroundCropResizeDialog : Dialog
    {
        private ILogger _log;
        public SKBitmap StartImage { get; set; }
        public SKBitmap FinalImage { get; set; }
        public bool SaveChanges { get; set; }

        // Image preview parameters
        private StackLayout _previewLayout;
        private SKBitmap _preview;
        private int _width;
        private int _height;
        private float _aspectRatio;

        private SKPoint _selectionAreaLocation = new();
        private SKPoint _imageLocation = new();

        private PointF? _lastCursorPoint;

        public BackgroundCropResizeDialog(SKBitmap startImage, int width, int height, ILogger log)
        {
            _log = log;
            Title = "Crop & Scale";
            Size = new(800, 650);
            StartImage = startImage;
            FinalImage = new(width, height);

            _width = StartImage.Width;
            _height = StartImage.Height;
            _aspectRatio = _width * _height;
            _preview = new SKBitmap(700, 500);
            _previewLayout = new() { Padding = 10 };
            
            Button saveButton = new() { Text = "Save" };
            Button cancelButton = new() { Text = "Cancel" };
            saveButton.Click += (sender, args) =>
            {
                SaveChanges = true;
                Close();
            };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            Button zoomAutoButton = new() { Text = "Auto", Width = 50 };
            NumericStepper widthStepper = new() { Value = StartImage.Width, MinValue = 1, Width = 55 };
            NumericStepper heightStepper = new() { Value = StartImage.Height, MinValue = 1, Width = 55 };
            CheckBox maintainAspectRatio = new() { Checked = true };
            zoomAutoButton.Click += (sender, args) =>
            {
                _width = FinalImage.Width;
                _height = FinalImage.Height;
                UpdateImage();
            };
            maintainAspectRatio.CheckedChanged += (sender, e) =>
            {
                if (maintainAspectRatio?.Checked is true)
                {
                    _aspectRatio = _width / _height;
                }
            };


            MouseWheel += OnMouseWheelUpdate;
            _previewLayout.MouseMove += OnMouseMove;

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    _previewLayout,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Spacing = 3,
                        Items =
                        {
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                Spacing = 3,
                                Items =
                                {
                                    ControlGenerator.GetControlWithLabel("Size",
                                    new StackLayout
                                    {
                                        Orientation = Orientation.Vertical,
                                        Items =
                                        {
                                            widthStepper,
                                            heightStepper,
                                        }
                                    }),
                                    ControlGenerator.GetControlWithLabel("Linked", maintainAspectRatio),
                                    zoomAutoButton,
                                }
                            },
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 3,
                                Items =
                                {
                                    saveButton,
                                    cancelButton,
                                }
                            }
                        }
                    }
                }
            };

            UpdateImage();
        }

        private void OnMouseWheelUpdate(object sender, MouseEventArgs e)
        {
            if (!e.Modifiers.HasFlag(Keys.Control))
            {
                return;
            }
            _width = (int) Math.Max(1, Math.Min(_preview.Width, _width + (e.Delta.Height * 10)));
            _height = (int) Math.Max(1, Math.Min(_preview.Height, _height + (e.Delta.Height * 10 * _aspectRatio)));
            UpdateImage();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            if (e.Buttons != MouseButtons.Primary)
            {
                _lastCursorPoint = e.Location;
                return;
            }
            _lastCursorPoint ??= e.Location;

            if (e.Modifiers.HasFlag(Keys.Control))
            {
                _imageLocation.Offset(
                    e.Location.X - _lastCursorPoint.Value.X,
                    e.Location.Y - _lastCursorPoint.Value.Y
                );
            }
            else
            {
                _selectionAreaLocation.Offset(
                    e.Location.X - _lastCursorPoint.Value.X,
                    e.Location.Y - _lastCursorPoint.Value.Y
                );
                _selectionAreaLocation.X = Math.Max(0, Math.Min(_preview.Width - FinalImage.Width, _selectionAreaLocation.X));
                _selectionAreaLocation.Y = Math.Max(0, Math.Min(_preview.Height - FinalImage.Height, _selectionAreaLocation.Y));
            }
            _lastCursorPoint = e.Location;
            UpdateImage();
        }

        private void UpdateImage()
        {
            SKCanvas previewCanvas = new(_preview);
            
            // Draw background
            previewCanvas.Clear();
            previewCanvas.DrawColor(SKColors.DarkGray);
            
            // Draw image
            previewCanvas.DrawBitmap(StartImage,
                new SKRect(0, 0, StartImage.Width, StartImage.Height),
                new SKRect(
                    _imageLocation.X, 
                    _imageLocation.Y,
                    _imageLocation.X + _width, 
                    _imageLocation.Y + _height
                )
            );

            // Update final canvas
            SKCanvas finalCanvas = new(FinalImage);
            SKRect areaRect = new(
                    _selectionAreaLocation.X,
                    _selectionAreaLocation.Y,
                    _selectionAreaLocation.X + FinalImage.Width,
                    _selectionAreaLocation.Y + FinalImage.Height
            );
            finalCanvas.DrawBitmap(_preview, areaRect, new SKRect(0, 0, FinalImage.Width, FinalImage.Height));

            // Draw UI, update image view
            previewCanvas.Flush();
            previewCanvas.DrawRect(areaRect, new() { Color = new(0xff, 0x00, 0x00, 0x55) });
            _previewLayout.Content = new ImageView() { Image = new SKGuiImage(_preview) };
        }
    }
}
