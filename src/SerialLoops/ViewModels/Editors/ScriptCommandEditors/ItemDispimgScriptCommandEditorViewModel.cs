using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ItemDispimgScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private readonly MainWindowViewModel _window;
    public EditorTabsPanelViewModel Tabs { get; }

    private ItemItem _item;

    public ItemItem Item
    {
        get => _item;
        set
        {
            this.RaiseAndSetIfChanged(ref _item, value);
            ((ItemScriptParameter)Command.Parameters[0]).ItemIndex = (short)_item.ItemIndex;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)_item.ItemIndex;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<LocalizedItemLocation> Locations { get; }
        = new(Enum.GetValues<ItemItem.ItemLocation>().Select(l => new LocalizedItemLocation(l)));
    private LocalizedItemLocation _location;
    public LocalizedItemLocation Location
    {
        get => _location;
        set
        {
            this.RaiseAndSetIfChanged(ref _location, value);
            ((ItemLocationScriptParameter)Command.Parameters[1]).Location = _location.Location;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)_location.Location;
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<LocalizedItemTransition> Transitions { get; }
        = new(Enum.GetValues<ItemItem.ItemTransition>().Select(t => new LocalizedItemTransition(t)));
    private LocalizedItemTransition _transition;
    public LocalizedItemTransition Transition
    {
        get => _transition;
        set
        {
            this.RaiseAndSetIfChanged(ref _transition, value);
            ((ItemTransitionScriptParameter)Command.Parameters[2]).Transition = _transition.Transition;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = (short)_transition.Transition;
            Script.UnsavedChanges = true;
        }
    }

    public ICommand ChangeItemCommand { get; }

    public ItemDispimgScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, MainWindowViewModel window)
        : base(command, scriptEditor, log)
    {
        _window = window;
        Tabs = _window.EditorTabs;
        _item = (ItemItem)_window.OpenProject.Items.FirstOrDefault(i =>
            i.Type == ItemDescription.ItemType.Item &&
            ((ItemItem)i).ItemIndex == ((ItemScriptParameter)Command.Parameters[0]).ItemIndex);
        _location = new(((ItemLocationScriptParameter)Command.Parameters[1]).Location);
        _transition = new(((ItemTransitionScriptParameter)Command.Parameters[2]).Transition);

        ChangeItemCommand = ReactiveCommand.CreateFromTask(ChangeItem);
    }

    private async Task ChangeItem()
    {
        GraphicSelectionDialogViewModel graphicSelectionDialog = new(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Item).Cast<ItemItem>(),
            Item, _window.OpenProject, _window.Log);
        ItemItem item = await new GraphicSelectionDialog { DataContext = graphicSelectionDialog }.ShowDialog<ItemItem>(_window.Window);
        if (item is not null)
        {
            Item = item;
        }
    }
}

public readonly struct LocalizedItemLocation(ItemItem.ItemLocation location)
{
    public ItemItem.ItemLocation Location { get; } = location;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(location.ToString());
}

public readonly struct LocalizedItemTransition(ItemItem.ItemTransition transition)
{
    public ItemItem.ItemTransition Transition { get; } = transition;
    public string DisplayText { get; } = Strings.ResourceManager.GetString(transition.ToString());
}
