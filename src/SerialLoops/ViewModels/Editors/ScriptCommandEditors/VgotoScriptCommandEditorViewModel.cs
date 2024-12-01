using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class VgotoScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log)
    : ScriptCommandEditorViewModel(command, scriptEditor, log)
{
    private string _conditional = ((ConditionalScriptParameter)command.Parameters[0]).Conditional;
    public string Conditional
    {
        get => _conditional;
        set
        {
            this.RaiseAndSetIfChanged(ref _conditional, value);
            ((ConditionalScriptParameter)Command.Parameters[0]).Conditional = value;
            if (Script.Event.ConditionalsSection.Objects.Contains(_conditional))
            {
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[0] =
                    (short)Script.Event.ConditionalsSection.Objects.IndexOf(_conditional);
            }
            else
            {
                Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[0] =
                    (short)(Script.Event.ConditionalsSection.Objects.Count - 1);
                Script.Event.ConditionalsSection.Objects.Insert(Script.Event.ConditionalsSection.Objects.Count - 1, _conditional);
            }
            Script.UnsavedChanges = true;
            Command.UpdateDisplay();
        }
    }

    private ReactiveScriptSection _sectionToJumpTo = scriptEditor.ScriptSections[command.Script.ScriptSections.IndexOf(((ScriptSectionScriptParameter)command.Parameters[1]).Section)];
    public ReactiveScriptSection SectionToJumpTo
    {
        get => _sectionToJumpTo;
        set
        {
            this.RaiseAndSetIfChanged(ref _sectionToJumpTo, value);
            ((ScriptSectionScriptParameter)Command.Parameters[1]).Section = _sectionToJumpTo.Section;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[1] = Script.Event.LabelsSection.Objects
                .FirstOrDefault(l => l.Name.Replace("/", "").Equals(_sectionToJumpTo.Name))?.Id ?? 0;
            Script.UnsavedChanges = true;
            Command.UpdateDisplay();
        }
    }
}
