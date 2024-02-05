using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class EditUiTextDialog : Dialog
    {
        private ILogger _log;
        private Project _project;

        public EditUiTextDialog(Project project, ILogger log)
        {
            _project = project;
            _log = log;

            Title = Application.Instance.Localize(this, "Edit UI Text");
            MinimumSize = new(400, 600);
            Size = new(400, 600);
            Padding = 10;
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            StackLayout uiTextLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
            };
            for (int i = 0; i < _project.UiText.Messages.Count; i++)
            {
                if (!UiDescriptions.ContainsKey(i))
                {
                    continue;
                }

                TextArea uiTextBox = new() { Width = 400, Height = 80, AcceptsReturn = true, AcceptsTab = false };
                if (_project.LangCode == "ja")
                {
                    uiTextBox.Text = _project.UiText.Messages[i];
                }
                else
                {
                    uiTextBox.Text = _project.UiText.Messages[i].GetSubstitutedString(_project);
                }

                int currentIndex = i;
                uiTextBox.TextChanged += (args, sender) =>
                {
                    if (_project.LangCode == "ja")
                    {
                        _project.UiText.Messages[currentIndex] = uiTextBox.Text;
                    }
                    else
                    {
                        _project.UiText.Messages[currentIndex] = uiTextBox.Text.GetOriginalString(_project);
                    }
                };

                string uiDescription = UiDescriptions[i];

                GroupBox uiTextGroup = new()
                {
                    Content = uiTextBox,
                    Text = uiDescription,
                };

                uiTextLayout.Items.Add(uiTextGroup);
            }

            Button saveButton = new() { Text = Application.Instance.Localize(this, "Save") };
            saveButton.Click += (sender, args) =>
            {
                _log.Log("Attempting to save UI text...");
                Lib.IO.WriteStringFile(Path.Combine("assets", "data", $"{_project.UiText.Index:X3}.s"), _project.UiText.GetSource(new()), _project, _log);
                Close();
            };

            Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            StackLayout buttonsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items =
                {
                    saveButton,
                    cancelButton,
                }
            };

            Content = new TableLayout
            {
                Spacing = new(5, 5),
                Rows =
                {
                    new(new Scrollable { Content = uiTextLayout, Height = 500 }),
                    new(buttonsLayout),
                },
            };
        }

        private static readonly Dictionary<int, string> UiDescriptions = new()
        {
            { 2, Application.Instance.Localize(null, "Main Topic") },
            { 3, Application.Instance.Localize(null, "Haruhi Topic") },
            { 4, Application.Instance.Localize(null, "Character Topic") },
            { 5, Application.Instance.Localize(null, "Sub-Topic") },
            { 6, Application.Instance.Localize(null, "Character Distribution") },
            { 7, Application.Instance.Localize(null, "Investigation Phase Results") },
            { 8, Application.Instance.Localize(null, "Companion Selection") },
            { 9, Application.Instance.Localize(null, "Character distribution instructions") },
            { 10, Application.Instance.Localize(null, "Title screen New Game ticker tape") },
            { 11, Application.Instance.Localize(null, "Title screen Load Game ticker tape") },
            { 12, Application.Instance.Localize(null, "Title screen Extra ticker tape") },
            { 13, Application.Instance.Localize(null, "Title screen Options ticker tape") },
            { 14, Application.Instance.Localize(null, "Pause menu Load ticker tape") },
            { 15, Application.Instance.Localize(null, "Pause menu Config ticker tape") },
            { 16, Application.Instance.Localize(null, "Pause menu Title ticker tape") },
            { 17, Application.Instance.Localize(null, "Episode 1 title") },
            { 18, Application.Instance.Localize(null, "Episode 2 title") },
            { 19, Application.Instance.Localize(null, "Episode 3 title") },
            { 20, Application.Instance.Localize(null, "Episode 4 title") },
            { 21, Application.Instance.Localize(null, "Episode 5 title") },
            { 22, Application.Instance.Localize(null, "Episode 1 ticker tape") },
            { 23, Application.Instance.Localize(null, "Episode 2 ticker tape") },
            { 24, Application.Instance.Localize(null, "Episode 3 ticker tape") },
            { 25, Application.Instance.Localize(null, "Episode 4 ticker tape") },
            { 26, Application.Instance.Localize(null, "Episode 5 ticker tape") },
            { 27, Application.Instance.Localize(null, "No save data ticker tape") },
            { 28, Application.Instance.Localize(null, "No saves Load Game menu ticker tape") },
            { 29, Application.Instance.Localize(null, "Load Game menu ticker tape") },
            { 30, Application.Instance.Localize(null, "Save game ticker tape") },
            { 31, Application.Instance.Localize(null, "Yes") },
            { 32, Application.Instance.Localize(null, "No") },
            { 33, Application.Instance.Localize(null, "Load this save prompt") },
            { 34, Application.Instance.Localize(null, "Save progress prompt message box") },
            { 35, Application.Instance.Localize(null, "Save progress prompt end game message box") },
            { 36, Application.Instance.Localize(null, "Save prompt") },
            { 37, Application.Instance.Localize(null, "Overwrite save prompt message box") },
            { 38, Application.Instance.Localize(null, "Loading prompt message box") },
            { 39, Application.Instance.Localize(null, "Saving prompt message box") },
            { 40, Application.Instance.Localize(null, "Accessing save data prompt message box") },
            { 41, Application.Instance.Localize(null, "Save loaded message box") },
            { 42, Application.Instance.Localize(null, "Game saved message box") },
            { 43, Application.Instance.Localize(null, "Title screen return unsaved progress lost prompt message box") },
            { 44, Application.Instance.Localize(null, "Try again prompt message box") },
            { 45, Application.Instance.Localize(null, "Save progress prompt message box") },
            { 46, Application.Instance.Localize(null, "Resetting save data message box") },
            { 47, Application.Instance.Localize(null, "Deleting all data message box") },
            { 48, Application.Instance.Localize(null, "Save game read fail message box") },
            { 49, Application.Instance.Localize(null, "Save game write fail message box") },
            { 50, Application.Instance.Localize(null, "Save data damaged & reset message box") },
            { 51, Application.Instance.Localize(null, "System data damaged & reset message box") },
            { 52, Application.Instance.Localize(null, "Save data 1 damaged & reset message box") },
            { 53, Application.Instance.Localize(null, "Save data 2 damaged & reset message box") },
            { 54, Application.Instance.Localize(null, "Quicksave data damaged & reset message box") },
            { 55, Application.Instance.Localize(null, "Companion selection description") },
            { 56, Application.Instance.Localize(null, "Kyon companion selected description") },
            { 57, Application.Instance.Localize(null, "Asahina companion selected description") },
            { 58, Application.Instance.Localize(null, "Nagato companion selected description") },
            { 59, Application.Instance.Localize(null, "Koizumi companion selected description") },
            { 60, Application.Instance.Localize(null, "Puzzle phase character description") },
            { 61, Application.Instance.Localize(null, "Asahina puzzle phase selected description") },
            { 62, Application.Instance.Localize(null, "Nagato puzzle phase selected description") },
            { 63, Application.Instance.Localize(null, "Koizumi puzzle phase selected description") },
            { 64, Application.Instance.Localize(null, "Sound (Options)") },
            { 65, Application.Instance.Localize(null, "Game Investigation Phase (Options)") },
            { 66, Application.Instance.Localize(null, "Game Puzzle Phase (Options)") },
            { 67, Application.Instance.Localize(null, "Reset options to default title") },
            { 68, Application.Instance.Localize(null, "Erase data options title") },
            { 69, Application.Instance.Localize(null, "Sound options ticker tape") },
            { 70, Application.Instance.Localize(null, "Investigation phase options ticker tape") },
            { 71, Application.Instance.Localize(null, "Puzzle phase options ticker tape") },
            { 72, Application.Instance.Localize(null, "Reset to default config ticker tape") },
            { 73, Application.Instance.Localize(null, "Erase data options ticker tape") },
            { 74, Application.Instance.Localize(null, "Background music volume options ticker tape") },
            { 75, Application.Instance.Localize(null, "Sound effect volume options ticker tape") },
            { 76, Application.Instance.Localize(null, "Unvoiced dialogue volume options ticker tape") },
            { 77, Application.Instance.Localize(null, "Voiced dialogue volume options ticker tape") },
            { 78, Application.Instance.Localize(null, "Character voice toggle options ticker tape") },
            { 79, Application.Instance.Localize(null, "Toggle Kyon's voice ticker tape") },
            { 80, Application.Instance.Localize(null, "Toggle Haruhi's voice ticker tape") },
            { 81, Application.Instance.Localize(null, "Toggle Mikuru's voice ticker tape") },
            { 82, Application.Instance.Localize(null, "Toggle Nagato's voice ticker tape") },
            { 83, Application.Instance.Localize(null, "Toggle Koizumi's voice ticker tape") },
            { 84, Application.Instance.Localize(null, "Toggle Kyon's sister's voice ticker tape") },
            { 85, Application.Instance.Localize(null, "Toggle Tsuruya's voice ticker tape") },
            { 86, Application.Instance.Localize(null, "Toggle Taniguchi's voice ticker tape") },
            { 87, Application.Instance.Localize(null, "Toggle Kunikida's voice ticker tape") },
            { 88, Application.Instance.Localize(null, "Toggle Mysterious Girl's voice ticker tape") },
            { 89, Application.Instance.Localize(null, "Reset settings to default ticker tape") },
            { 90, Application.Instance.Localize(null, "All data will be erased prompt message box") },
            { 91, Application.Instance.Localize(null, "Second prompt on data being erased") },
            { 92, Application.Instance.Localize(null, "Data reset message box") },
            { 93, Application.Instance.Localize(null, "All data erased message box") },
            { 94, Application.Instance.Localize(null, "Batch Dialogue Display option") },
            { 95, Application.Instance.Localize(null, "Dialogue Skipping setting") },
            { 96, Application.Instance.Localize(null, "Puzzle Interrupt Scenes setting") },
            { 97, Application.Instance.Localize(null, "Topic Stock Mode option") },
            { 98, Application.Instance.Localize(null, "Batch Dialogue Display ticker tape") },
            { 99, Application.Instance.Localize(null, "Dialogue Skipping ticker tape") },
            { 100, Application.Instance.Localize(null, "Puzzle Interrupt Scenes ticker tape") },
            { 101, Application.Instance.Localize(null, "Topic Stock Mode ticker tape") },
            { 102, Application.Instance.Localize(null, "Batch Dialogue Display Off") },
            { 103, Application.Instance.Localize(null, "Batch Dialogue Display On") },
            { 104, Application.Instance.Localize(null, "Puzzle Interrupt Scenes Off") },
            { 105, Application.Instance.Localize(null, "Puzzle Interrupt Scenes Unseen Only") },
            { 106, Application.Instance.Localize(null, "Puzzle Interrupt Scenes On") },
            { 107, Application.Instance.Localize(null, "Dialogue Skipping Fast Forward") },
            { 108, Application.Instance.Localize(null, "Dialogue Skipping Skip Already Read") },
            { 109, Application.Instance.Localize(null, "Kyon's Dialogue Box Group Selection") },
            { 111, Application.Instance.Localize(null, "Group selection impossible selection made ticker tape") },
            { 112, Application.Instance.Localize(null, "Kyon's Dialogue Box Companion Selection") },
            { 115, Application.Instance.Localize(null, "Chess Mode Unlocked message box") },
            { 116, Application.Instance.Localize(null, "Haruhi Suzumiya event unlocked message box") },
            { 117, Application.Instance.Localize(null, "Mikuru Asahina event unlocked message box") },
            { 118, Application.Instance.Localize(null, "Yuki Nagato event unlocked message box") },
            { 119, Application.Instance.Localize(null, "Itsuki Koizumi event unlocked message box") },
            { 120, Application.Instance.Localize(null, "Tsuruya event unlocked message box") },
            { 121, Application.Instance.Localize(null, "Collected all Haruhi topics message box") },
            { 122, Application.Instance.Localize(null, "Collected all Mikuru topics message box") },
            { 123, Application.Instance.Localize(null, "Collected all Nagato topics message box") },
            { 124, Application.Instance.Localize(null, "Collected all Koizumi topics message box") },
            { 125, Application.Instance.Localize(null, "Collected all main topics message box") },
            { 126, Application.Instance.Localize(null, "Cleared all chess puzzles message box") },
            { 127, Application.Instance.Localize(null, "100%'d game message box") },
            { 128, Application.Instance.Localize(null, "Extras unlocked message box") },
            { 129, Application.Instance.Localize(null, "Mystery girl voice added to config message box") },
        };
    }
}
