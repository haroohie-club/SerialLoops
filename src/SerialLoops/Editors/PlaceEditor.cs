﻿using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

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
            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    new SKGuiImage(_place.GetPreview(_project)),
                }
            };
        }
    }
}
