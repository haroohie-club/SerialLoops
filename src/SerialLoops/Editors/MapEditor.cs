﻿using Eto.Forms;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Editors
{
    internal class MapEditor : Editor
    {

        private MapItem _item;

        public MapEditor(MapItem map) : base(map)
        {
            _item = map;
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