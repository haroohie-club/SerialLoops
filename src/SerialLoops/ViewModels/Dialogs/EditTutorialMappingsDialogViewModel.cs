using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;
using IO = SerialLoops.Lib.IO;

namespace SerialLoops.ViewModels.Dialogs;

public class EditTutorialMappingsDialogViewModel : ViewModelBase
{
    public ObservableCollection<TutorialMapping> Tutorials { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditTutorialMappingsDialogViewModel(Project project, EditorTabsPanelViewModel tabs, ILogger log)
    {
        Tutorials = new(project.TutorialFile.Tutorials.Where(t => t.AssociatedScript > 0)
            .Select(t => new TutorialMapping(t, project, tabs)));

        SaveCommand = ReactiveCommand.Create<EditTutorialMappingsDialog>(dialog =>
        {
            foreach (TutorialMapping tutorial in Tutorials)
            {
                project.TutorialFile.Tutorials.First(t => t.Id == tutorial.Flag)
                    .AssociatedScript = (short)tutorial.Script.Event.Index;
            }
            IO.WriteStringFile(Path.Combine("assets", "events", $"{project.TutorialFile.Index:X3}.s"),
                project.TutorialFile.GetSource([]), project, log);
            dialog.Close();
        });
        CancelCommand = ReactiveCommand.Create<EditTutorialMappingsDialog>(dialog => dialog.Close());
    }
}

public class TutorialMapping(Tutorial tutorial, Project project, EditorTabsPanelViewModel tabs) : ReactiveObject
{
    public EditorTabsPanelViewModel Tabs { get; } = tabs;

    public ObservableCollection<ScriptItem> Scripts { get; } = new(project.Items
        .Where(i => i.Type == ItemDescription.ItemType.Script)
        .Cast<ScriptItem>());

    [Reactive]
    public int Flag { get; set; } = tutorial.Id;

    public string TutorialName => $"Tutorial {Flag}";

    [Reactive]
    public ScriptItem Script { get; set; } = (ScriptItem)project.Items.First(i =>
        i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == tutorial.AssociatedScript);
}
