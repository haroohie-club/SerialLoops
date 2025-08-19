using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using MsBox.Avalonia.Enums;
using ReactiveHistory;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.Utility;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors.ScenarioCommandEditors;
using SerialLoops.Views.Dialogs;
using static HaruhiChokuretsuLib.Archive.Event.ScenarioCommand;

namespace SerialLoops.ViewModels.Editors;

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
            if (_selectedCommand is not null)
            {
                CurrentCommandViewModel = GetScenarioCommandEditor(SelectedCommand);
            }
            else
            {
                CurrentCommandViewModel = null;
            }
        }
    }
    [Reactive]
    public ScenarioCommandEditorViewModel CurrentCommandViewModel { get; set; }

    private StackHistory _history;

    public ICommand AddCommand { get; set; }
    public ICommand DeleteCommand { get; set; }
    public ICommand ClearCommand { get; set; }
    public ICommand UpCommand { get; set; }
    public ICommand DownCommand { get; set; }

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public KeyGesture UndoGesture { get; }
    public KeyGesture RedoGesture { get; }

    public ScenarioEditorViewModel(ScenarioItem scenario, MainWindowViewModel window, ILogger log) : base(scenario, window, log)
    {
        _history = new();

        _scenario = scenario;
        Commands = new(scenario.ScenarioCommands.Select((s, i) => new PrettyScenarioCommand(s, i, scenario)));
        AddCommand = ReactiveCommand.Create(Add);
        DeleteCommand = ReactiveCommand.Create(Delete);
        ClearCommand = ReactiveCommand.CreateFromTask(Clear);
        UpCommand = ReactiveCommand.Create(Up);
        DownCommand = ReactiveCommand.Create(Down);

        UndoCommand = ReactiveCommand.Create(() => _history.Undo());
        RedoCommand = ReactiveCommand.Create(() => _history.Redo());
        UndoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Z);
        RedoGesture = GuiExtensions.CreatePlatformAgnosticCtrlGesture(Key.Y);
    }

    private async void Add()
    {
        int selectedIndex = Math.Min(_scenario.Scenario.Commands.Count - 1, Commands.IndexOf(SelectedCommand));
        ScenarioVerb? newVerb = await new AddScenarioCommandDialog { DataContext = new AddScenarioCommandDialogViewModel() }.ShowDialog<ScenarioVerb?>(Window.Window);
        if (newVerb is not null)
        {
            int param = newVerb switch
            {
                ScenarioVerb.NEW_GAME => 1,
                ScenarioVerb.PUZZLE_PHASE => ((PuzzleItem)Window.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Puzzle)).Puzzle.Index,
                ScenarioVerb.LOAD_SCENE => ((ScriptItem)Window.OpenProject.Items.First(i => i.Type == ItemDescription.ItemType.Script)).Event.Index,
                _ => 0,
            };
            ScenarioCommand newCommand = new((ScenarioVerb)newVerb, param);
            _scenario.Scenario.Commands.Insert(selectedIndex + 1, newCommand);
            Commands.Insert(selectedIndex + 1, new(_scenario.GetCommandMacro(newCommand), selectedIndex + 1, _scenario));
            if (SelectedCommand is not null)
            {
                SelectedCommand.CommandIndex = Commands.IndexOf(SelectedCommand);
            }
            Description.UnsavedChanges = true;

            _history.Snapshot(() =>
            {
                _scenario.Scenario.Commands.RemoveAt(selectedIndex + 1);
                Commands.RemoveAt(selectedIndex + 1);
            }, () =>
            {
                _scenario.Scenario.Commands.Insert(selectedIndex + 1, newCommand);
                Commands.Insert(selectedIndex + 1, new(_scenario.GetCommandMacro(newCommand), selectedIndex + 1, _scenario));
            });
        }
    }
    private void Delete()
    {
        int selectedIndex = Commands.IndexOf(SelectedCommand);
        if (selectedIndex < 0 || selectedIndex >= _scenario.Scenario.Commands.Count)
        {
            return;
        }

        PrettyScenarioCommand prettyCommand = Commands[selectedIndex];
        ScenarioCommand command = _scenario.Scenario.Commands[selectedIndex];

        Commands.RemoveAt(selectedIndex);
        _scenario.Scenario.Commands.RemoveAt(selectedIndex);

        Description.UnsavedChanges = true;

        _history.Snapshot(() =>
        {
            Commands.Insert(selectedIndex, prettyCommand);
            _scenario.Scenario.Commands.Insert(selectedIndex, command);
        }, () =>
        {
            Commands.RemoveAt(selectedIndex);
            _scenario.Scenario.Commands.RemoveAt(selectedIndex);
        });
    }
    private async Task Clear()
    {
        if (await Window.Window.ShowMessageBoxAsync(Strings.ScenarioEditorClearScenarioLabel, Strings.ScenarioEditorClearCommandsMessage,
                ButtonEnum.YesNo, Icon.Warning, _log) == ButtonResult.Yes)
        {
            Commands.Clear();
            _scenario.Scenario.Commands.Clear();

            Description.UnsavedChanges = true;
        }
    }
    private void Up()
    {
        int selectedIndex = Commands.IndexOf(SelectedCommand);
        if (selectedIndex <= 0 || selectedIndex >= _scenario.Scenario.Commands.Count)
        {
            return;
        }

        Commands.Swap(selectedIndex, selectedIndex - 1);
        _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex - 1);
        SelectedCommand = Commands.ElementAt(selectedIndex - 1);

        Description.UnsavedChanges = true;

        _history.Snapshot(() =>
        {
            Commands.Swap(selectedIndex, selectedIndex - 1);
            _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex - 1);
        }, () =>
        {
            Commands.Swap(selectedIndex, selectedIndex - 1);
            _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex - 1);
        });
    }
    private void Down()
    {
        int selectedIndex = Commands.IndexOf(SelectedCommand);
        if (selectedIndex < 0 || selectedIndex >= _scenario.Scenario.Commands.Count - 1)
        {
            return;
        }

        Commands.Swap(selectedIndex, selectedIndex + 1);
        _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex + 1);
        SelectedCommand = Commands.ElementAt(selectedIndex + 1);

        Description.UnsavedChanges = true;

        _history.Snapshot(() =>
        {
            Commands.Swap(selectedIndex, selectedIndex + 1);
            _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex + 1);
        }, () =>
        {
            Commands.Swap(selectedIndex, selectedIndex + 1);
            _scenario.Scenario.Commands.Swap(selectedIndex, selectedIndex + 1);
        });
    }

    private ScenarioCommandEditorViewModel GetScenarioCommandEditor(PrettyScenarioCommand command)
    {
        return command.Verb switch
        {
            ScenarioVerb.LOAD_SCENE => new LoadSceneScenarioCommandEditorViewModel(command, Window.OpenProject, Window.EditorTabs, _history),
            ScenarioVerb.PUZZLE_PHASE => new PuzzlePhaseScenarioCommandEditorViewModel(command, Window.OpenProject, Window.EditorTabs, _history),
            ScenarioVerb.ROUTE_SELECT => new RouteSelectScenarioCommandEditorViewModel(command, Window.OpenProject, Window.EditorTabs, _history),
            _ => new(command, Window.EditorTabs, _history),
        };
    }
}
