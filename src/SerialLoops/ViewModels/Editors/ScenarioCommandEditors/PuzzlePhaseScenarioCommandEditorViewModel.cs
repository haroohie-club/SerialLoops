using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Models;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScenarioCommandEditors;

public class PuzzlePhaseScenarioCommandEditorViewModel : ScenarioCommandEditorViewModel
{
    public ObservableCollection<PuzzleItem> Puzzles { get; set; }

    private PuzzleItem _puzzle;
    public PuzzleItem Puzzle
    {
        get => _puzzle;
        set
        {
            this.RaiseAndSetIfChanged(ref _puzzle, value);
            _parameter = _puzzle.Puzzle.Index;
            SelectedScenarioCommand.Parameter = _puzzle.DisplayName;
            SelectedScenarioCommand.Scenario.Scenario.Commands[SelectedScenarioCommand.CommandIndex].Parameter = _parameter;
            SelectedScenarioCommand.Scenario.UnsavedChanges = true;
        }
    }

    public PuzzlePhaseScenarioCommandEditorViewModel(PrettyScenarioCommand command, Project project, EditorTabsPanelViewModel tabs) : base(command, tabs)
    {
        Puzzles = new(project.Items.Where(i => i.Type == ItemDescription.ItemType.Puzzle).Cast<PuzzleItem>());
        _puzzle = Puzzles.FirstOrDefault(s => s.DisplayName == command.Parameter);
        _parameter = _puzzle.Puzzle.Index;
    }
}