using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Models;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScenarioCommandEditors;

public class ScenarioCommandEditorViewModel : ViewModelBase
{
    public EditorTabsPanelViewModel Tabs { get; private set; }

    [Reactive]
    public PrettyScenarioCommand SelectedScenarioCommand { get; set; }

    protected int _parameter;
    public int Parameter
    {
        get => _parameter;
        set
        {
            this.RaiseAndSetIfChanged(ref _parameter, value);
            SelectedScenarioCommand.Parameter = $"{_parameter:N0}";
            SelectedScenarioCommand.Scenario.Scenario.Commands[SelectedScenarioCommand.CommandIndex].Parameter = _parameter;
            SelectedScenarioCommand.Scenario.UnsavedChanges = true;
        }
    }

    public ScenarioCommandEditorViewModel(PrettyScenarioCommand command, EditorTabsPanelViewModel tabs)
    {
        SelectedScenarioCommand = command;
        if (!int.TryParse(command.Parameter, out _parameter))
        {
            _parameter = 0;
        }
        Tabs = tabs;
    }

    public short MaxValue => short.MaxValue;
}