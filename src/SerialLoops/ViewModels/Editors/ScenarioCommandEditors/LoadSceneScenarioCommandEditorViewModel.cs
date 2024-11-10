using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScenarioCommandEditors;

public class LoadSceneScenarioCommandEditorViewModel : ScenarioCommandEditorViewModel
{
    public ObservableCollection<ScriptItem> Scripts { get; set; }

    private ScriptItem _scene;
    public ScriptItem Scene
    {
        get => _scene;
        set
        {
            this.RaiseAndSetIfChanged(ref _scene, value);
            _parameter = _scene.Event.Index;
            SelectedScenarioCommand.Parameter = _scene.DisplayName;
            SelectedScenarioCommand.Scenario.Scenario.Commands[SelectedScenarioCommand.CommandIndex].Parameter = _parameter;
            SelectedScenarioCommand.Scenario.UnsavedChanges = true;
        }
    }

    public LoadSceneScenarioCommandEditorViewModel(PrettyScenarioCommand command, Project project, EditorTabsPanelViewModel tabs) : base(command, tabs)
    {
        Scripts = new(project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Cast<ScriptItem>());
        _scene = Scripts.FirstOrDefault(s => s.DisplayName == command.Parameter);
        _parameter = _scene.Event.Index;
    }
}