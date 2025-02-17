using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChibiEmoteScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public ObservableCollection<ChibiItem> Chibis { get; } = new(scriptEditor.Window.OpenProject.Items
        .Where(i => i.Type == ItemDescription.ItemType.Chibi).Cast<ChibiItem>());
    public ChibiItem Chibi
    {
        get => ((ChibiScriptParameter)Command.Parameters[0]).Chibi;
        set
        {
            ((ChibiScriptParameter)Command.Parameters[0]).Chibi = value;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)value.TopScreenIndex;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }

    public ObservableCollection<LocalizedChibiEmote> ChibiEmotes { get; } = new(Enum
        .GetValues<ChibiEmoteScriptParameter.ChibiEmote>()
        .Select(e => new LocalizedChibiEmote(e)));

    public LocalizedChibiEmote ChibiEmote
    {
        get => new(((ChibiEmoteScriptParameter)Command.Parameters[1]).Emote);
        set
        {
            ((ChibiEmoteScriptParameter)Command.Parameters[1]).Emote = value.Emote;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = (short)value.Emote;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            Script.UnsavedChanges = true;
        }
    }
}

public struct LocalizedChibiEmote(ChibiEmoteScriptParameter.ChibiEmote chibiEmote)
{
    public ChibiEmoteScriptParameter.ChibiEmote Emote { get; } = chibiEmote;

    public string DisplayName => Strings.ResourceManager.GetString(Emote.ToString());
}
