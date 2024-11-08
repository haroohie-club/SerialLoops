using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScenarioCommandEditors;

public class RouteSelectScenarioCommandEditorViewModel : ScenarioCommandEditorViewModel
{
    public ObservableCollection<GroupSelectionItem> GroupSelections { get; set; }

    public GroupSelectionItem _groupSelection;
    public GroupSelectionItem GroupSelection
    {
        get => _groupSelection;
        set
        {
            this.RaiseAndSetIfChanged(ref _groupSelection, value);
            _parameter = GroupSelections.IndexOf(_groupSelection);
            SelectedScenarioCommand.Parameter = _groupSelection.DisplayName;
            SelectedScenarioCommand.Scenario.Scenario.Commands[SelectedScenarioCommand.CommandIndex].Parameter = _parameter;
            SelectedScenarioCommand.Scenario.UnsavedChanges = true;
        }
    }

    public RouteSelectScenarioCommandEditorViewModel(PrettyScenarioCommand command, Project project, EditorTabsPanelViewModel tabs) : base(command, tabs)
    {
        GroupSelections = new(project.Items.Where(i => i.Type == ItemDescription.ItemType.Group_Selection).Cast<GroupSelectionItem>());
        _groupSelection = GroupSelections.FirstOrDefault(g => g.DisplayName == command.Parameter);
        _parameter = GroupSelections.IndexOf(_groupSelection);
    }
}