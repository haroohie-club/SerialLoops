using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.ViewModels.Editors.ScenarioCommandEditors;
using SixLabors.ImageSharp;

namespace SerialLoops.ViewModels.Editors
{
    public class ScenarioEditorViewModel : EditorViewModel
    {
        private ScenarioItem _scenario;
        private PrettyScenarioCommand _selectedCommand;

        public ObservableCollection<PrettyScenarioCommand> Commands { get; set; }

        public PrettyScenarioCommand SelectedCommand
        {
            get => _selectedCommand;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCommand, value);
                CurrentCommandViewModel = GetScenarioCommandEditor(SelectedCommand);
            }
        }
        [Reactive]
        public ScenarioCommandEditorViewModel CurrentCommandViewModel { get; set; }
        public ICommand AddCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand ClearCommand { get; set; }
        public ICommand UpCommand { get; set; }
        public ICommand DownCommand { get; set; }

        public ScenarioEditorViewModel(ScenarioItem scenario, MainWindowViewModel window, ILogger log) : base(scenario, window, log)
        {
            _scenario = scenario;
            Commands = new(scenario.ScenarioCommands.Select(s => new PrettyScenarioCommand(s)));
            AddCommand = ReactiveCommand.Create(Add);
            DeleteCommand = ReactiveCommand.Create(Delete);
            ClearCommand = ReactiveCommand.Create(Clear);
            UpCommand = ReactiveCommand.Create(Up);
            DownCommand = ReactiveCommand.Create(Down);
        }

        private void Add()
        {

        }
        private void Delete()
        {

        }
        private void Clear()
        {

        }
        private void Up()
        {

        }
        private void Down()
        {

        }
        public static ScenarioCommandEditorViewModel GetScenarioCommandEditor(PrettyScenarioCommand command)
        {
            return new();
        }
    }
}
