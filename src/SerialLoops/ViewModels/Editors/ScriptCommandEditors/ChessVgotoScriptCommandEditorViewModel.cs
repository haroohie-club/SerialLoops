using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class ChessVgotoScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    private ReactiveScriptSection _clearSection = scriptEditor.ScriptSections.ElementAtOrDefault(
        command.Script.ScriptSections.IndexOf(((ScriptSectionScriptParameter)command.Parameters[0]).Section));
    public ReactiveScriptSection ClearSection
    {
        get => _clearSection;
        set
        {
            this.RaiseAndSetIfChanged(ref _clearSection, value);
            ((ScriptSectionScriptParameter)Command.Parameters[0]).Section = _clearSection.Section;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = Script.Event.LabelsSection.Objects
                .FirstOrDefault(l => l.Name.Replace("/", "").Equals(_clearSection.Name))?.Id ?? 0;
            Script.UnsavedChanges = true;
            Command.UpdateDisplay();
        }
    }

    private ReactiveScriptSection _missSection = scriptEditor.ScriptSections.ElementAtOrDefault(
        command.Script.ScriptSections.IndexOf(((ScriptSectionScriptParameter)command.Parameters[1]).Section));
    public ReactiveScriptSection MissSection
    {
        get => _missSection;
        set
        {
            this.RaiseAndSetIfChanged(ref _missSection, value);
            ((ScriptSectionScriptParameter)Command.Parameters[1]).Section = _missSection.Section;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = Script.Event.LabelsSection.Objects
                .FirstOrDefault(l => l.Name.Replace("/", "").Equals(_missSection.Name))?.Id ?? 0;
            Script.UnsavedChanges = true;
            Command.UpdateDisplay();
        }
    }

    private ReactiveScriptSection _miss2Section = scriptEditor.ScriptSections.ElementAtOrDefault(
        command.Script.ScriptSections.IndexOf(((ScriptSectionScriptParameter)command.Parameters[2]).Section));
    public ReactiveScriptSection Miss2Section
    {
        get => _miss2Section;
        set
        {
            this.RaiseAndSetIfChanged(ref _miss2Section, value);
            ((ScriptSectionScriptParameter)Command.Parameters[2]).Section = _miss2Section.Section;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[2] = Script.Event.LabelsSection.Objects
                .FirstOrDefault(l => l.Name.Replace("/", "").Equals(_miss2Section.Name))?.Id ?? 0;
            Script.UnsavedChanges = true;
            Command.UpdateDisplay();
        }
    }
}
