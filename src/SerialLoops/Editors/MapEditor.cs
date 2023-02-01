using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class MapEditor : Editor
    {
        public MapEditor(MapItem map, ILogger log) : base(map, log)
        {
        }

        public override Panel GetEditorPanel()
        {
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
