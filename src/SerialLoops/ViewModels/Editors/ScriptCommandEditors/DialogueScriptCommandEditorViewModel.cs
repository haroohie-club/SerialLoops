using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Archive.Event;
using ReactiveUI;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors
{
    public partial class DialogueScriptCommandEditorViewModel : ScriptCommandEditorViewModel
    {
        private MainWindowViewModel _window;
        public EditorTabsPanelViewModel Tabs { get; set; }
        private Func<ItemDescription, bool> _specialPredicate = (i => true);

        public DialogueScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, MainWindowViewModel window) : base(command, scriptEditor)
        {
            _window = window;
            Tabs = _window.EditorTabs;
            _speaker = ((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker;
            _specialPredicate = i => i.Name != "NONE" && ((CharacterSpriteItem)i).Sprite.Character == _speaker;
            _dialogueLine = ((DialogueScriptParameter)command.Parameters[0]).Line.Text;
            _characterSprite = ((SpriteScriptParameter)command.Parameters[1]).Sprite;
            SelectCharacterSpriteCommand = ReactiveCommand.CreateFromTask(SelectCharacterSpriteCommand_Executed);
        }

        public ObservableCollection<string> Speakers { get; set; } = new(Enum.GetNames(typeof(Speaker)));
        private Speaker _speaker;
        public string Speaker
        {
            get => _speaker.ToString();
            set
            {
                this.RaiseAndSetIfChanged(ref _speaker, Enum.Parse<Speaker>(value));
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker = _speaker;
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Speaker = _speaker;
                // Set other dropdowns
                _specialPredicate = i => i.Name != "NONE" && ((CharacterSpriteItem)i).Sprite.Character == _speaker;
                Script.UnsavedChanges = true;
                ScriptEditor.UpdatePreview();
            }
        }

        private string _dialogueLine;
        public string DialogueLine
        {
            get => _dialogueLine.GetSubstitutedString(_window.OpenProject);
            set
            {
                string text = value;
                text = StartStringQuotes().Replace(text, "“");
                text = MidStringOpenQuotes().Replace(text, "$1“");
                text = text.Replace('"', '”');

                this.RaiseAndSetIfChanged(ref _dialogueLine, text);

                if (string.IsNullOrEmpty(_dialogueLine))
                {
                    ((DialogueScriptParameter)Command.Parameters[0]).Line.Text = "";
                    Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Text = "";
                    ((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer = 0;
                    Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Pointer = 0;
                }
                else
                {
                    if (((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer == 0)
                    {
                        // It doesn't matter what we set this to as long as it's greater than zero
                        // The ASM creation routine only checks that the pointer is not zero
                        ((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer = 1;
                        Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Pointer = 1;
                    }
                    string originalText = text.GetOriginalString(_window.OpenProject);
                    ((DialogueScriptParameter)Command.Parameters[0]).Line.Text = originalText;
                    Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Text = originalText;
                }

                ScriptEditor.UpdatePreview();
                Script.UnsavedChanges = true;
            }
        }

        private CharacterSpriteItem _characterSprite;
        public CharacterSpriteItem CharacterSprite
        {
            get => _characterSprite;
            set
            {
                this.RaiseAndSetIfChanged(ref _characterSprite, value);
                if (_characterSprite is null)
                {
                    ((SpriteScriptParameter)Command.Parameters[1]).Sprite = null;
                    Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[1] = 0;
                }
                else
                {
                    ((SpriteScriptParameter)Command.Parameters[1]).Sprite = _characterSprite;
                    Script.Event.ScriptSections[Script.Event.ScriptSections.IndexOf(Command.Section)]
                        .Objects[Command.Index].Parameters[1] = (short)(_characterSprite.Index);
                }
                ScriptEditor.UpdatePreview();
                Script.UnsavedChanges = true;
            }
        }
        public ICommand SelectCharacterSpriteCommand { get; }

        private async Task SelectCharacterSpriteCommand_Executed()
        {
            GraphicSelectionDialogViewModel graphicSelectionDialog = new(_window.OpenProject.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Cast<CharacterSpriteItem>(),
                CharacterSprite, _window.OpenProject, _window.Log, _specialPredicate);
            CharacterSpriteItem sprite = await new GraphicSelectionDialog() { DataContext = graphicSelectionDialog }.ShowDialog<CharacterSpriteItem>(_window.Window);
            if (sprite is not null)
            {
                CharacterSprite = sprite;
            }
        }

        [GeneratedRegex(@"^""")]
        private static partial Regex StartStringQuotes();
        [GeneratedRegex(@"(\s)""")]
        private static partial Regex MidStringOpenQuotes();
    }
}
