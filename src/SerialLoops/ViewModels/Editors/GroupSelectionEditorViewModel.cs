using HaruhiChokuretsuLib.Util;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;

namespace SerialLoops.ViewModels.Editors
{
    public class GroupSelectionEditorViewModel : EditorViewModel
    {
        [Reactive]
        public GroupSelectionItem GroupSelection { get; set; }
        public Project OpenProject { get; set; }

        public GroupSelectionEditorViewModel(GroupSelectionItem groupSelection, MainWindowViewModel window, ILogger log) : base(groupSelection, window, log)
        {
            GroupSelection = groupSelection;
            OpenProject = window.OpenProject;
        }
    }
}
