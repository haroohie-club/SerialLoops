using Eto.Forms;
using Eto.Forms.Controls.SkiaSharp.Shared;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;

namespace SerialLoops.Editors
{
    public class LayoutEditor(LayoutItem layoutItem, ILogger log) : Editor(layoutItem, log)
    {
        private LayoutItem _layout;
        private int _currentSelectedEntryIndex;
        private SKBitmap _layoutSourcePreview;
        // If in the future we can make GL support work fully, we should switch this to an SKGLControl
        private SKControl _layoutScreen;
        private ImageView _layoutSource;

        private const string SCREENX = "Screen X";
        private const string SCREENY = "Screen Y";
        private const string SCREENW = "Screen W";
        private const string SCREENH = "Screen H";
        private const string SOURCEX = "Source X";
        private const string SOURCEY = "Source Y";
        private const string SOURCEW = "Source W";
        private const string SOURCEH = "Source H";

        public override Container GetEditorPanel()
        {
            _layout = (LayoutItem)Description;

            _layoutScreen = new() { Width = 256, Height = 192, PaintSurfaceAction = ScreenPaint };
            _layoutSource = new();

            TableLayout layoutEntriesTable = new();
            for (int i = _layout.StartEntry; i < _layout.StartEntry + _layout.NumEntries; i++)
            {
                DropDown graphicSelector = new() { Tag = i, Enabled = false };
                graphicSelector.Items.Add(new ListItem
                {
                    Key = "-1",
                    Text = "NONE",
                });
                graphicSelector.Items.AddRange(_layout.GraphicsFiles.Select((g, idx) => new ListItem
                {
                    Key = idx.ToString(),
                    Text = g.Name,
                }));
                graphicSelector.SelectedKey = _layout.Layout.LayoutEntries[i].RelativeShtxIndex.ToString();
                graphicSelector.GotFocus += LayoutControl_GotFocus;
                graphicSelector.SelectedKeyChanged += GraphicSelector_SelectedKeyChanged;

                NumericStepper screenXStepper = new() { Tag = i, ID = $"{SCREENX}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].ScreenX };
                NumericStepper screenYStepper = new() { Tag = i, ID = $"{SCREENY}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].ScreenY };
                NumericStepper screenWStepper = new() { Tag = i, ID = $"{SCREENW}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].ScreenW };
                NumericStepper screenHStepper = new() { Tag = i, ID = $"{SCREENH}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].ScreenH };
                NumericStepper sourceXStepper = new() { Tag = i, ID = $"{SOURCEX}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].TextureX };
                NumericStepper sourceYStepper = new() { Tag = i, ID = $"{SOURCEY}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].TextureY };
                NumericStepper sourceWStepper = new() { Tag = i, ID = $"{SOURCEW}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].TextureW };
                NumericStepper sourceHStepper = new() { Tag = i, ID = $"{SOURCEH}{i}", MaximumDecimalPlaces = 0, Value = _layout.Layout.LayoutEntries[i].TextureH };

                screenXStepper.GotFocus += LayoutControl_GotFocus;
                screenYStepper.GotFocus += LayoutControl_GotFocus;
                screenWStepper.GotFocus += LayoutControl_GotFocus;
                screenHStepper.GotFocus += LayoutControl_GotFocus;
                sourceXStepper.GotFocus += LayoutControl_GotFocus;
                sourceYStepper.GotFocus += LayoutControl_GotFocus;
                sourceWStepper.GotFocus += LayoutControl_GotFocus;
                sourceHStepper.GotFocus += LayoutControl_GotFocus;

                screenXStepper.ValueChanged += LayoutStepper_ValueChanged;
                screenYStepper.ValueChanged += LayoutStepper_ValueChanged;
                screenWStepper.ValueChanged += LayoutStepper_ValueChanged;
                screenHStepper.ValueChanged += LayoutStepper_ValueChanged;
                sourceXStepper.ValueChanged += LayoutStepper_ValueChanged;
                sourceYStepper.ValueChanged += LayoutStepper_ValueChanged;
                sourceWStepper.ValueChanged += LayoutStepper_ValueChanged;
                sourceHStepper.ValueChanged += LayoutStepper_ValueChanged;

                ColorPicker tintColorPicker = new() { Tag = i, Value = _layout.Layout.LayoutEntries[i].Tint.ToEtoDrawingColor() };
                tintColorPicker.ValueChanged += TintColorPicker_ValueChanged;

                layoutEntriesTable.Rows.Add(new(
                    new(graphicSelector, scaleWidth: true),
                    new(screenXStepper, scaleWidth: true),
                    new(screenYStepper, scaleWidth: true),
                    new(screenWStepper, scaleWidth: true),
                    new(screenHStepper, scaleWidth: true),
                    new(sourceXStepper, scaleWidth: true),
                    new(sourceYStepper, scaleWidth: true),
                    new(sourceWStepper, scaleWidth: true),
                    new(sourceHStepper, scaleWidth: true),
                    new(tintColorPicker, scaleWidth: true)
                    ));
                layoutEntriesTable.Rows.Add(new()); // prevent last row from stretching
            }

            Button saveLayoutPreviewButton = new() { Text = Application.Instance.Localize(this, "Save Layout Preview") };
            saveLayoutPreviewButton.Click += (sender, args) =>
            {
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "PNG Image"), Extensions = [".png"] });
                if (saveFileDialog.ShowAndReportIfFileSelected(this))
                {
                    using FileStream fs = File.Create(saveFileDialog.FileName);
                    _layout.GetLayoutImage().Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                }
            };
            Button saveSourcePreviewButton = new() { Text = Application.Instance.Localize(this, "Save Source Preview") };
            saveSourcePreviewButton.Click += (sender, args) =>
            {
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.Filters.Add(new() { Name = Application.Instance.Localize(this, "PNG Image"), Extensions = [".png"] });
                if (_layoutSourcePreview is not null)
                {
                    if (saveFileDialog.ShowAndReportIfFileSelected(this))
                    {
                        using FileStream fs = File.Create(saveFileDialog.FileName);
                        _layoutSourcePreview.Encode(fs, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    }
                }
                else
                {
                    MessageBox.Show(Application.Instance.Localize(this, "No source image selected!"), Application.Instance.Localize(this, "No source image!"), MessageBoxType.Warning);
                }
            };

            return new TableLayout
                (
                    new TableRow(
                        new TableCell(new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                _layoutScreen,
                                _layoutSource,
                                new StackLayout
                                {
                                    Orientation = Orientation.Vertical,
                                    Spacing = 3,
                                    Items =
                                    {
                                        saveLayoutPreviewButton,
                                        saveSourcePreviewButton,
                                    }
                                }
                            }
                        })
                    ),
                    new TableRow(
                        new TableLayout(
                            new TableRow(
                                new TableCell("Graphic", scaleWidth: true),
                                new TableCell(SCREENX, scaleWidth: true),
                                new TableCell(SCREENY, scaleWidth: true),
                                new TableCell(SCREENW, scaleWidth: true),
                                new TableCell(SCREENH, scaleWidth: true),
                                new TableCell(SOURCEX, scaleWidth: true),
                                new TableCell(SOURCEY, scaleWidth: true),
                                new TableCell(SOURCEW, scaleWidth: true),
                                new TableCell(SOURCEH, scaleWidth: true),
                                new TableCell("Tint", scaleWidth: true)
                                )
                            )
                        ),
                    new TableRow(
                        new TableCell(new Scrollable
                        {
                            Content = layoutEntriesTable,
                        })
                    )
                );
        }
        private void ScreenPaint(SKSurface surface)
        {
            SKCanvas canvas = surface.Canvas;
            canvas.Clear();

            for (int i = _layout.StartEntry; i < _layout.StartEntry + _layout.NumEntries; i++)
            {
                (SKBitmap tile, SKRect dest) = _layout.GetLayoutEntryRender(i);
                if (tile is not null)
                {
                    canvas.DrawBitmap(tile, dest);
                    if (_currentSelectedEntryIndex == i)
                    {
                        canvas.DrawRect(dest, new() { Color = SKColors.Red, StrokeWidth = 2f, Style = SKPaintStyle.Stroke });
                    }
                }
            }
            canvas.Flush();
        }

        private void UpdateScreenLayout()
        {
            _layoutScreen.Invalidate();
        }

        private void UpdateSourceLayout(int index)
        {
            if (_layout.Layout.LayoutEntries[index].RelativeShtxIndex < 0)
            {
                _layoutSource.Image = new SKGuiImage(new(1, 1));
                _layoutSourcePreview = null;
            }
            else
            {
                SKBitmap sourceBitmap = _layout.GraphicsFiles[_layout.Layout.LayoutEntries[index].RelativeShtxIndex].GetImage();
                SKCanvas canvas = new(sourceBitmap);
                canvas.DrawRect(_layout.Layout.LayoutEntries[index].GetTileBounds(), new() { Color = SKColors.Red, StrokeWidth = 2f, Style = SKPaintStyle.Stroke });
                canvas.Flush();
                _layoutSource.Image = new SKGuiImage(sourceBitmap);
                _layoutSourcePreview = sourceBitmap;
            }
            _currentSelectedEntryIndex = index;
        }

        private void LayoutControl_GotFocus(object sender, EventArgs e)
        {
            CommonControl control = (CommonControl)sender;
            int index = (int)control.Tag;
            UpdateSourceLayout(index);
            UpdateScreenLayout();
        }

        private void GraphicSelector_SelectedKeyChanged(object sender, EventArgs e)
        {
            DropDown dropDown = (DropDown)sender;
            int layoutIndex = (int)dropDown.Tag;
            short relativeGrpIndex = short.Parse(dropDown.SelectedKey);
            _layout.Layout.LayoutEntries[layoutIndex].RelativeShtxIndex = relativeGrpIndex;

            UpdateSourceLayout(layoutIndex);
            UpdateScreenLayout();
            UpdateTabTitle(false);
        }


        private void LayoutStepper_ValueChanged(object sender, EventArgs e)
        {
            NumericStepper stepper = (NumericStepper)sender;
            int index = (int)stepper.Tag;
            string target = stepper.ID.Replace(index.ToString(), "");

            switch (target)
            {
                case SCREENX:
                    _layout.Layout.LayoutEntries[index].ScreenX = (short)stepper.Value;
                    break;
                case SCREENY:
                    _layout.Layout.LayoutEntries[index].ScreenY = (short)stepper.Value;
                    break;
                case SCREENW:
                    _layout.Layout.LayoutEntries[index].ScreenW = (short)stepper.Value;
                    break;
                case SCREENH:
                    _layout.Layout.LayoutEntries[index].ScreenH = (short)stepper.Value;
                    break;
                case SOURCEX:
                    _layout.Layout.LayoutEntries[index].TextureX = (short)stepper.Value;
                    UpdateSourceLayout(index);
                    break;
                case SOURCEY:
                    _layout.Layout.LayoutEntries[index].TextureY = (short)stepper.Value;
                    UpdateSourceLayout(index);
                    break;
                case SOURCEW:
                    _layout.Layout.LayoutEntries[index].TextureW = (short)stepper.Value;
                    UpdateSourceLayout(index);
                    break;
                case SOURCEH:
                    _layout.Layout.LayoutEntries[index].TextureH = (short)stepper.Value;
                    UpdateSourceLayout(index);
                    break;
            }
            UpdateScreenLayout();
            UpdateTabTitle(false);
        }

        private void TintColorPicker_ValueChanged(object sender, EventArgs e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            int index = (int)colorPicker.Tag;

            _layout.Layout.LayoutEntries[index].Tint = colorPicker.Value.ToSKColor();
            UpdateScreenLayout();
            UpdateTabTitle(false);
        }
    }
}
