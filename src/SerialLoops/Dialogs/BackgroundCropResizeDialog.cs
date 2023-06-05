using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
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
        private float _aspectRatio;

        private NumericStepper _widthStepper;
        private NumericStepper _heightStepper;
        private SKPoint _selectionAreaLocation = new();
        private SKPoint _imageLocation = new();
        
        private PointF? _lastCursorPoint;

        public BackgroundCropResizeDialog(SKBitmap startImage, int width, int height, ILogger log)
        {
            _log = log;
            Title = "Crop & Scale";
            MinimumSize = new(900, 650);
            StartImage = startImage;
            FinalImage = new(width, height);

            _preview = new SKBitmap(650, 550);
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

            _widthStepper = new() { Value = StartImage.Width, MinValue = 1, Width = 55 };
            _heightStepper = new() { Value = StartImage.Height, MinValue = 1, Width = 55 };
            _aspectRatio = (float) _widthStepper.Value / (float) _heightStepper.Value;
            Button scaleToFitButton = new() { Text = "Apply" };
            CheckBox preserveAspectRatioCheckbox = new() { Checked = true };
            scaleToFitButton.Click += (sender, args) =>
            {
                _widthStepper.Value = FinalImage.Width;
                _heightStepper.Value = FinalImage.Height;
                UpdateImage();
            };
            preserveAspectRatioCheckbox.CheckedChanged += (sender, e) =>
            {
                if (preserveAspectRatioCheckbox.Checked is true)
                {
                    _aspectRatio = (float) _widthStepper.Value / (float) _heightStepper.Value;
                }
                UpdateImage();
            };
            _widthStepper.ValueChanged += (sender, e) =>
            {
                if (preserveAspectRatioCheckbox is {Checked: true})
                {
                    _heightStepper.Value = (int) Math.Round(_widthStepper.Value / _aspectRatio);
                }
                UpdateImage();
            };
            _heightStepper.ValueChanged += (sender, e) =>
            {
                if (preserveAspectRatioCheckbox is {Checked: true})
                {
                    _widthStepper.Value = (int) Math.Round(_heightStepper.Value * _aspectRatio);
                }
                UpdateImage();
            };


            MouseWheel += OnMouseWheelUpdate;
            KeyDown += OnKeyDown;
            _previewLayout.MouseMove += OnMouseMove;

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Padding = 5,
                        Items =
                        {
                            _previewLayout,
                            new GroupBox
                            {
                                Text = "Scale Image",
                                Padding = 5,
                                Content = new StackLayout
                                {
                                    Orientation = Orientation.Vertical,
                                    Spacing = 5,
                                    Padding = 5,
                                    Items =
                                    {
                                        ControlGenerator.GetControlWithLabel("Size:",
                                            new StackLayout
                                            {
                                                Orientation = Orientation.Vertical,
                                                Items =
                                                {
                                                    _widthStepper,
                                                    _heightStepper,
                                                }
                                            }),
                                        ControlGenerator.GetControlWithLabel("Preserve Aspect Ratio:",
                                            preserveAspectRatioCheckbox),
                                        ControlGenerator.GetControlWithLabel("Scale To Fit:", scaleToFitButton),
                                    }
                                }
                            }
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 3,
                        Padding = 5,
                        Items =
                        {
                            saveButton,
                            cancelButton,
                        }
                    }
                }
            };

            UpdateImage();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            int xChange = 0;
            int yChange = 0;
            switch (e.KeyData)
            {
                case Keys.Down:
                    yChange = 10;
                    break;
                case Keys.Left:
                    xChange = -10;
                    break;
                case Keys.Right:
                    xChange = 10;
                    break;
                case Keys.Up:
                    yChange = -10;
                    break;
            }

            if (!e.Modifiers.HasFlag(Keys.Shift))
            {
                xChange /= 10;
                yChange /= 10;
            }

            if (e.Modifiers.HasFlag(Keys.Control))
            {
                _selectionAreaLocation.Offset(xChange, yChange);
            }
            else
            {
                _imageLocation.Offset(xChange, yChange);
            }
            UpdateImage();
        }

        private void OnMouseWheelUpdate(object sender, MouseEventArgs e)
        {
            if (!e.Modifiers.HasFlag(Keys.Control))
            {
                return;
            }
            _widthStepper.Value = (int) Math.Max(1, Math.Min(_preview.Width, _widthStepper.Value + e.Delta.Height * 10));
            _heightStepper.Value = (int) Math.Max(1, Math.Min(_preview.Height, _heightStepper.Value + e.Delta.Height * 10 * _aspectRatio));
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
                    _imageLocation.X + (int) _widthStepper.Value, 
                    _imageLocation.Y + (int) _heightStepper.Value
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
            SKBitmap uiBitmap = new(_preview.Width, _preview.Height);
            SKCanvas uiCanvas = new(uiBitmap);
            uiCanvas.Clear();
            uiCanvas.DrawRect(new SKRect(0, 0, _preview.Width, _preview.Height), new() { Color = new(0x00, 0x00, 0x00, 0x00) });
            uiCanvas.ClipRect(areaRect);
            uiCanvas.Flush();
            previewCanvas.DrawBitmap(uiBitmap, 0, 0);
            
            previewCanvas.Flush();
            _previewLayout.Content = new ImageView() { Image = new SKGuiImage(_preview) };
        }
    }
}
