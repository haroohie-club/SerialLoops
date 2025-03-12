using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class TopicGetScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    public EditorTabsPanelViewModel Tabs { get; }

    [Reactive]
    public short TopicId { get; set; }

    public ObservableCollection<TopicItem> Topics { get; }
    private TopicItem _selectedTopic;
    public TopicItem SelectedTopic
    {
        get => _selectedTopic;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTopic, value);
            ((TopicScriptParameter)Command.Parameters[0]).TopicId = _selectedTopic.TopicEntry.Id;
            TopicId = _selectedTopic.TopicEntry.Id;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = _selectedTopic.TopicEntry.Id;
            Script.UnsavedChanges = true;
        }
    }

    public ICommand SelectTopicCommand { get; }

    public TopicGetScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window)
        : base(command, scriptEditor, log)
    {
        Tabs = window.EditorTabs;
        Topics = new(window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Topic).Cast<TopicItem>());
        TopicId = ((TopicScriptParameter)Command.Parameters[0]).TopicId;
        _selectedTopic = (TopicItem)window.OpenProject.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).TopicEntry.Id == TopicId);
        SelectTopicCommand = ReactiveCommand.Create(() => SelectedTopic = Topics.FirstOrDefault());
    }
}
