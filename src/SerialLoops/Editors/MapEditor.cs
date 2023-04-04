using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;

namespace SerialLoops.Editors
{
    public class MapEditor : Editor
    {
        private MapItem _map;

        public MapEditor(MapItem map, Project project, ILogger log) : base(map, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _map = (MapItem)Description;

            StackLayout mapLayout = new()
            {
                Items =
                {
                    new SKGuiImage(_map.GetMapImage(_project.Grp, false, false)),
                }
            };

            CheckBox drawPathingMapBox = new() { Text = "Draw Pathing Map", Checked = false };
            CheckBox drawStartingPointBox = new() { Text = "Draw Starting Point", Checked = false };

            void redrawMap(object obj, EventArgs args)
            {
                mapLayout.Content = new SKGuiImage(_map.GetMapImage(_project.Grp, drawPathingMapBox.Checked ?? true, drawStartingPointBox.Checked ?? true));
            }

            drawPathingMapBox.CheckedChanged += redrawMap;
            drawStartingPointBox.CheckedChanged += redrawMap;

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    mapLayout,
                    ControlGenerator.GetControlWithIcon(drawPathingMapBox, "Pathing", _log),
                    ControlGenerator.GetControlWithIcon(drawStartingPointBox, "Camera", _log)
,
                }
            };
        }
    }
}
