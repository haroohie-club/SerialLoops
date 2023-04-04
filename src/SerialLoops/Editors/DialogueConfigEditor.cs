using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Linq;

namespace SerialLoops.Editors
{
    public class DialogueConfigEditor : Editor
    {
        private DialogueConfigItem _dialogueConfig;

        public DialogueConfigEditor(DialogueConfigItem dialogueConfig, ILogger log) : base(dialogueConfig, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _dialogueConfig = (DialogueConfigItem)Description;

            DropDown characterDropDown = new();
            characterDropDown.Items.AddRange(Enum.GetNames<Speaker>().Select(c => new ListItem { Key = c.ToString(), Text = c.ToString() }));
            characterDropDown.SelectedKey = _dialogueConfig.DialogueConfig.Character.ToString();

            return new StackLayout
            {
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Character", characterDropDown),
                    ControlGenerator.GetControlWithLabel("Voice Font", new NumericStepper { Value = _dialogueConfig.DialogueConfig.VoiceFont }),
                    ControlGenerator.GetControlWithLabel("Text Timer", new NumericStepper { Value = _dialogueConfig.DialogueConfig.TextTimer }),
                },
            };
        }
    }
}
