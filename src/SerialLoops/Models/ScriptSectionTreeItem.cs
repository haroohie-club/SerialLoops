using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Script;

namespace SerialLoops.Models
{
    public class ScriptSectionTreeItem(ScriptSection section, List<ScriptItemCommand> commands) : ITreeItem
    {
        private ScriptSection _section = section;
        public ScriptSection Section
        {
            get => _section;
            set
            {
                _section = value;
                Text = _section.Name;
            }
        }

        public string Text { get; set; } = section.Name;
        public Avalonia.Svg.Svg Icon { get; set; } = null;
        public ObservableCollection<ITreeItem> Children { get; set; } = new([.. commands.Select(c => new ScriptCommandTreeItem(c))]);
        public bool IsExpanded { get; set; } = true;

        public Control GetDisplay()
        {
            StackPanel panel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Margin = new(2),
            };
            panel.Children.Add(new TextBlock { Text = Text });
            return panel;
        }
    }
}
