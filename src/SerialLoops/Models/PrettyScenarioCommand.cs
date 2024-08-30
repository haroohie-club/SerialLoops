using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Models
{
    public class PrettyScenarioCommand((ScenarioCommand.ScenarioVerb Verb, string Parameter) scenarioCommand)
    {
        public ScenarioCommand.ScenarioVerb Verb { get; set; } = scenarioCommand.Verb;
        public string Parameter { get; set; } = scenarioCommand.Parameter;
    }
}
