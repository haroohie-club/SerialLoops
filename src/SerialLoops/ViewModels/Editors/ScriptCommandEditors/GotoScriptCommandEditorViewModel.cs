﻿using System.Linq;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public class GotoScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private ReactiveScriptSection _sectionToJumpTo;
    public ReactiveScriptSection SectionToJumpTo
    {
        get => _sectionToJumpTo;
        set
        {
            this.RaiseAndSetIfChanged(ref _sectionToJumpTo, value);
            ((ScriptSectionScriptParameter)Command.Parameters[0]).Section = _sectionToJumpTo.Section;
            Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                .Objects[Command.Index].Parameters[0] = Script.Event.LabelsSection.Objects
                .FirstOrDefault(l => l.Name.Replace("/", "").Equals(_sectionToJumpTo.Name))?.Id ?? 0;
            Script.UnsavedChanges = true;
            Command.UpdateDisplay();
        }
    }

    public GotoScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log) :
        base(command, scriptEditor, log)
    {
        _sectionToJumpTo = ScriptEditor.ScriptSections[Script.Event.ScriptSections.IndexOf(((ScriptSectionScriptParameter)command.Parameters[0]).Section)];
    }
}
