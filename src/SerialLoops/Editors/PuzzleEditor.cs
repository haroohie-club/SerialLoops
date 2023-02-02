using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using System.Linq;

namespace SerialLoops.Editors
{
    public class PuzzleEditor : Editor
    {
        private PuzzleItem _puzzle;

        public PuzzleEditor(PuzzleItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            _puzzle = (PuzzleItem)Description;
            StackLayout mainLayout = new()
            {
                Orientation = Orientation.Vertical
            };

            StackLayout topics = new() { Orientation = Orientation.Horizontal, Padding = new(8, 0) };
            foreach (var topic in _puzzle.Puzzle.AssociatedTopics)
            {
                topics.Items.Add($"{topic.Topic}");
            }
            mainLayout.Items.Add(topics);

            foreach (PuzzleHaruhiRoute haruhiRoute in _puzzle.Puzzle.HaruhiRoutes)
            {
                mainLayout.Items.Add(haruhiRoute.ToString());
            }

            return mainLayout;
        }
    }
}
