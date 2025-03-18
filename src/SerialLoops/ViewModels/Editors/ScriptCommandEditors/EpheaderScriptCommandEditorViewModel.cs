using System;
using System.Collections.ObjectModel;
using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class EpheaderScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    public ObservableCollection<LocalizedEpisode> Episodes { get; } =
        new(Enum.GetValues<EpisodeHeaderScriptParameter.Episode>().Select(e => new LocalizedEpisode(e)));

    public LocalizedEpisode SelectedEpisode
    {
        get => Episodes.First(e => e.Episode == ((EpisodeHeaderScriptParameter)Command.Parameters[0]).EpisodeHeaderIndex);
        set
        {
            ((EpisodeHeaderScriptParameter)Command.Parameters[0]).EpisodeHeaderIndex = value.Episode;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = (short)value.Episode;
            this.RaisePropertyChanged();
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }
}

public class LocalizedEpisode(EpisodeHeaderScriptParameter.Episode episode)
{
    public EpisodeHeaderScriptParameter.Episode Episode { get; set; } = episode;

    public string DisplayName => Strings.ResourceManager.GetString(Episode.ToString());
}
