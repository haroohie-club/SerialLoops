using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;

namespace SerialLoops.ViewModels.Controls;

public class ScenarioCommandPickerViewModel(short commandIndex, ScenarioItem scenario) : ViewModelBase
{
    public ScenarioItem Scenario { get; set; } = scenario;
    private short _scenarioCommandIndex = commandIndex;
    public short ScenarioCommandIndex
    {
        get => _scenarioCommandIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _scenarioCommandIndex, value);
            ScenarioCommandText = $"{Scenario.ScenarioCommands[_scenarioCommandIndex].Command} {Scenario.ScenarioCommands[_scenarioCommandIndex].Parameter}";
        }
    }

    [Reactive]
    public string ScenarioCommandText { get; set; } =
        $"{scenario.ScenarioCommands[commandIndex].Command} {scenario.ScenarioCommands[commandIndex].Parameter}";

    public ObservableCollection<string> ScenarioCommands { get; set; } =
        new(scenario.ScenarioCommands.Select(c => $"{c.Command} {c.Parameter}").ToArray());
}
