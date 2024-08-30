using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Models;

namespace SerialLoops.ViewModels.Editors
{
    public class ScenarioEditorViewModel : EditorViewModel
    {
        private ScenarioItem _scenario;

        public ObservableCollection<PrettyScenarioCommand> Commands { get; set; }

        //public ICommand CommandsBoxSelectionChanged { get; set; }

        public ScenarioEditorViewModel(ScenarioItem scenario, MainWindowViewModel window, ILogger log) : base(scenario, window, log)
        {
            _scenario = scenario;
            Commands = new(scenario.ScenarioCommands.Select(s => new PrettyScenarioCommand(s)));
            //CommandsBoxSelectionChanged = ReactiveCommand.Create<int>(SelectedCommandChanged);
        }


    }
}
