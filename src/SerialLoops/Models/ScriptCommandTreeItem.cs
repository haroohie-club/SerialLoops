using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Script;

namespace SerialLoops.Models
{
    public class ScriptCommandTreeItem(ScriptItemCommand command) : ITreeItem
    {
        private ScriptItemCommand _command = command;
        public ScriptItemCommand Command
        {
            get => _command;
            set
            {
                _command = value;
                Text = _command.ToString();
            }
        }

        [Reactive]
        public string Text { get; set; } = command.ToString();
        public Avalonia.Svg.Svg Icon { get; set; } = null;
        public ObservableCollection<ITreeItem> Children { get; set; } = null;
        public bool IsExpanded { get; set; } = false;

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
