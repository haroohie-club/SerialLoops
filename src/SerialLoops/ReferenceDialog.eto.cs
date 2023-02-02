using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System.Collections.Generic;

namespace SerialLoops
{
    partial class ReferenceDialog : FindItemsDialog
    {
        public ReferenceMode Mode;
        public ItemDescription Item;
        
        void InitializeComponent()
        {
            Title = Mode == ReferenceMode.REFERENCES_TO ? $"View item references" : $"View items referenced by";
            MinimumSize = new Size(400, 275);
            Padding = 10;

            List<ItemDescription> results = GetResults();
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Spacing = 10,
                Padding = 10,
                Items =
                {
                    $"{results.Count} files that {(Mode == ReferenceMode.REFERENCES_TO ? "reference" : "are referenced by")} this item:",
                    new ItemResultsPanel(results, Log) { Dialog = this }
                }
            };
        }

        private List<ItemDescription> GetResults()
        {
            return new List<ItemDescription>(); //todo
        }

        public enum ReferenceMode
        {
            REFERENCES_TO,
            REFERENCED_BY
        }

    }

}
