using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Topten.RichTextKit;

namespace SerialLoops.Editors
{
    public class PlaceEditor : Editor
    {
        private PlaceItem _place;

        public PlaceEditor(PlaceItem placeItem, Project project, ILogger log) : base(placeItem, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _place = (PlaceItem)Description;
            if (string.IsNullOrEmpty(_place.PlaceName))
            {
                _place.PlaceName = _place.DisplayName[4..];
            }

            TextBox placeTextBox = new() { Text = _place.DisplayName[4..] };

            StackLayout previewPanel = new()
            {
                Items =
                {
                    new SKGuiImage(_place.GetPreview(_project)),
                },
            };

            using Stream typefaceStream = Assembly.GetCallingAssembly().GetManifestResourceStream("SerialLoops.Graphics.MS-Gothic-Haruhi.ttf");
            SKTypeface msGothicHaruhi = SKTypeface.FromStream(typefaceStream);
            if (!PlaceItem.CustomFontMapper.HasFont())
            {
                PlaceItem.CustomFontMapper.AddFont(msGothicHaruhi);
            }

            placeTextBox.TextChanged += (sender, args) =>
            {
                _place.PlaceName = placeTextBox.Text;

                previewPanel.Items.Clear();
                previewPanel.Items.Add(new SKGuiImage(_place.GetNewPlaceGraphic(msGothicHaruhi)));
                
                UpdateTabTitle(false);
            };

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Place Name", placeTextBox),
                    previewPanel,
                }
            };
        }
    }
}
