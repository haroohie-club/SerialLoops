using Avalonia.Controls;
using HaruhiChokuretsuLib.Util;
using MiniToolbar.Avalonia;
using SerialLoops.Utility;
using System.Windows.Input;

namespace SerialLoops.Models
{
    public class EditorCommand
    {
        public ICommand Command { get; }
        public NativeMenuItem MenuItem { get; }
        public ToolbarButton ToolbarItem { get; }

        public EditorCommand(string menuText, string toolbarText, string iconName, ICommand command, ILogger log)
        {
            Command = command;

            MenuItem = new()
            {
                Header = menuText,
                Icon = ControlGenerator.GetIcon(iconName, log),
                Command = Command,
            };
            ToolbarItem = new()
            {
                Text = toolbarText,
                Icon = ControlGenerator.GetIcon(iconName, log, 32),
                Command = Command,
            };
        }
    }
}
