using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors
{
    public class PinMnlScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, Project project) : ScriptCommandEditorViewModel(command, scriptEditor)
    {
        private Project _project = project;

        private CharacterItem _speaker;
        public CharacterItem Speaker
        {
            get => _speaker;
            set
            {
                this.RaiseAndSetIfChanged(ref _speaker, value);
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker = _speaker.MessageInfo.Character;
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Speaker = _speaker.MessageInfo.Character;
                Script.UnsavedChanges = true;
                ScriptEditor.UpdatePreview();
            }
        }

        private string _dialogue;
        public string Dialogue
        {
            get => _dialogue.GetSubstitutedString(_project);
            set
            {

            }
        }
    }
}
