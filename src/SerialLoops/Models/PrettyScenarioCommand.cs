using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;

namespace SerialLoops.Models;

public class PrettyScenarioCommand(
    (ScenarioCommand.ScenarioVerb Verb, string Parameter) scenarioCommand,
    int index,
    ScenarioItem scenario) : ReactiveObject
{
    public ScenarioItem Scenario { get; set; } = scenario;
    public int CommandIndex { get; set; } = index;

    public ScenarioCommand.ScenarioVerb Verb { get; set; } = scenarioCommand.Verb;
    [Reactive]
    public string Parameter { get; set; } = scenarioCommand.Parameter;
}