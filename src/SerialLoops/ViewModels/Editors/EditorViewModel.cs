using System.Collections.Generic;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors
{
    public class EditorViewModel(ItemDescription item, MainWindowViewModel window, ILogger log, Project project = null, EditorTabsPanelViewModel tabs = null, ItemExplorerPanelViewModel explorer = null) : ViewModelBase
    {
        protected MainWindowViewModel _window = window;
        protected ILogger _log = log;
        protected Project _project = project;
        protected EditorTabsPanelViewModel _tabs = tabs;
        protected ItemExplorerPanelViewModel _explorer = explorer;

        public ItemDescription Description { get; set; } = item;
        public List<EditorCommand> EditorCommands { get; }

        public string IconSource => $"avares://SerialLoops/Assets/Icons/{Description.Type.ToString().Replace(' ', '_')}.png";
    }
}
