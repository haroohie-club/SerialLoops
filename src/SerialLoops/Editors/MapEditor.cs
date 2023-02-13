using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;

namespace SerialLoops.Editors
{
    public class MapEditor : Editor
    {
        private MapItem _map;

        public MapEditor(MapItem map, ILogger log, IProgressTracker tracker) : base(map, log, tracker)
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
                    new Label { Text = "Map Editor..todo!" }
                }
            };
        }
    }
}
