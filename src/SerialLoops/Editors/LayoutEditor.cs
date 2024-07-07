using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class LayoutEditor(LayoutItem layoutItem, ILogger log) : Editor(layoutItem, log)
    {
        private LayoutItem _layout;
        private ImageView _layoutScreen, _layoutSource;

        private const string SCREENX = "ScreenX";
        private const string SCREENY = "ScreenY";
        private const string SCREENW = "ScreenW";
        private const string SCREENH = "ScreenH";
        private const string SOURCEX = "SourceX";
        private const string SOURCEY = "SourceY";
        private const string SOURCEW = "SourceW";
        private const string SOURCEH = "SourceH";

        public override Container GetEditorPanel()
        {
            _layout = (LayoutItem)Description;

            _layoutScreen = new() { Image = new SKGuiImage(_layout.GetLayoutImage()) };
            _layoutSource = new();

            TableLayout layoutEntriesTable = new();
            for (int i = _layout.StartEntry; i < _layout.StartEntry + _layout.NumEntries; i++)
            {
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
                    new(screenXStepper),
                    new(screenYStepper),
                    new(screenWStepper),
                    new(screenHStepper),
                    new(sourceXStepper),
                    new(sourceYStepper),
                    new(sourceWStepper),
                    new(sourceHStepper),
                    new(tintColorPicker)
                    ));
            }

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
                            }
                        })
                    ),
                    new TableRow(
                        new TableCell(new Scrollable
                        {
                            Content = layoutEntriesTable,
                        })
                    )
                );
        }

        private void UpdateScreenLayout()
        {
            _layoutScreen.Image = new SKGuiImage(_layout.GetLayoutImage());
        }

        private void LayoutControl_GotFocus(object sender, System.EventArgs e)
        {
            CommonControl control = (CommonControl)sender;
            int index = (int)control.Tag;

            _layoutSource.Image = new SKGuiImage(_layout.GraphicsFiles[_layout.Layout.LayoutEntries[index].RelativeShtxIndex].GetImage());
        }

        private void LayoutStepper_ValueChanged(object sender, System.EventArgs e)
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
                    break;
                case SOURCEY:
                    _layout.Layout.LayoutEntries[index].TextureY = (short)stepper.Value;
                    break;
                case SOURCEW:
                    _layout.Layout.LayoutEntries[index].TextureW = (short)stepper.Value;
                    break;
                case SOURCEH:
                    _layout.Layout.LayoutEntries[index].TextureH = (short)stepper.Value;
                    break;
            }
            
            UpdateScreenLayout();
        }

        private void TintColorPicker_ValueChanged(object sender, System.EventArgs e)
        {
            ColorPicker colorPicker = (ColorPicker)sender;
            int index = (int)colorPicker.Tag;

            _layout.Layout.LayoutEntries[index].Tint = colorPicker.Value.ToSKColor();
            UpdateScreenLayout();
        }
    }
}
