using Eto.Forms;
using SerialLoops.Lib.Items;

namespace SerialLoops.Editors
{
    public class MapEditor : Editor
    {

        private MapItem _map;

        public MapEditor(MapItem map) : base(map)
        {
            _map = map;
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
