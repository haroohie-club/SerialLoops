using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

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

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    new SKGuiImage(_map.GetMapWithGrid(_project.Grp))
                }
            };
        }
    }
}
