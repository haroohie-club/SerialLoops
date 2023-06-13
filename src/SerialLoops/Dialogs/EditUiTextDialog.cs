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

            Title = "Edit UI Text";
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

            Button saveButton = new() { Text = "Save" };
            saveButton.Click += (sender, args) =>
            {
                _log.Log("Attempting to save UI text...");
                Lib.IO.WriteStringFile(Path.Combine("assets", "data", $"{_project.UiText.Index:X3}.s"), _project.UiText.GetSource(new()), _project, _log);
                Close();
            };

            Button cancelButton = new() { Text = "Cancel" };
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
            { 2, "Main Topic" },
            { 3, "Haruhi Topic" },
            { 4, "Character Topic" },
            { 5, "Sub-Topic" },
            { 6, "Character Distribution" },
            { 7, "Investigation Phase Results" },
            { 8, "Companion Selection" },
            { 9, "Character distribution instructions" },
            { 10, "Title screen New Game ticker tape" },
            { 11, "Title screen Load Game ticker tape" },
            { 12, "Title screen Extra ticker tape" },
            { 13, "Title screen Options ticker tape" },
            { 14, "Pause menu Load ticker tape" },
            { 15, "Pause menu Config ticker tape" },
            { 16, "Pause menu Title ticker tape" },
            { 17, "Episode 1 title" },
            { 18, "Episode 2 title" },
            { 19, "Episode 3 title" },
            { 20, "Episode 4 title" },
            { 21, "Episode 5 title" },
            { 22, "Episode 1 ticker tape" },
            { 23, "Episode 2 ticker tape" },
            { 24, "Episode 3 ticker tape" },
            { 25, "Episode 4 ticker tape" },
            { 26, "Episode 5 ticker tape" },
            { 27, "No save data ticker tape" },
            { 28, "No saves Load Game menu ticker tape" },
            { 29, "Load Game menu ticker tape" },
            { 30, "Save game ticker tape" },
            { 31, "Yes" },
            { 32, "No" },
            { 33, "Load this save prompt" },
            { 34, "Save progress prompt message box" },
            { 35, "Save progress prompt end game message box" },
            { 36, "Save prompt" },
            { 37, "Overwrite save prompt message box" },
            { 38, "Loading prompt message box" },
            { 39, "Saving prompt message box" },
            { 40, "Accessing save data prompt message box" },
            { 41, "Save loaded message box" },
            { 42, "Game saved message box" },
            { 43, "Title screen return unsaved progress lost prompt message box" },
            { 44, "Try again prompt message box" },
            { 45, "Save progress prompt message box" },
            { 46, "Resetting save data message box" },
            { 47, "Deleting all data message box" },
            { 48, "Save game read fail message box" },
            { 49, "Save game write fail message box" },
            { 50, "Save data damaged & reset message box" },
            { 51, "System data damaged & reset message box" },
            { 52, "Save data 1 damaged & reset message box" },
            { 53, "Save data 2 damaged & reset message box" },
            { 54, "Quicksave data damaged & reset message box" },
            { 55, "Companion selection description" },
            { 56, "Kyon companion selected description" },
            { 57, "Asahina companion selected description" },
            { 58, "Nagato companion selected description" },
            { 59, "Koizumi companion selected description" },
            { 60, "Puzzle phase character description" },
            { 61, "Asahina puzzle phase selected description" },
            { 62, "Nagato puzzle phase selected description" },
            { 63, "Koizumi puzzle phase selected description" },
            { 64, "Sound (Options)" },
            { 65, "Game Investigation Phase (Options)" },
            { 66, "Game Puzzle Phase (Options)" },
            { 67, "Reset options to default title" },
            { 68, "Erase data options title" },
            { 69, "Sound options ticker tape" },
            { 70, "Investigation phase options ticker tape" },
            { 71, "Puzzle phase options ticker tape" },
            { 72, "Reset to default config ticker tape" },
            { 73, "Erase data options ticker tape" },
            { 74, "Background music volume options ticker tape" },
            { 75, "Sound effect volume options ticker tape" },
            { 76, "Unvoiced dialogue volume options ticker tape" },
            { 77, "Voiced dialogue volume options ticker tape" },
            { 78, "Character voice toggle options ticker tape" },
            { 79, "Toggle Kyon's voice ticker tape" },
            { 80, "Toggle Haruhi's voice ticker tape" },
            { 81, "Toggle Mikuru's voice ticker tape" },
            { 82, "Toggle Nagato's voice ticker tape" },
            { 83, "Toggle Koizumi's voice ticker tape" },
            { 84, "Toggle Kyon's sister's voice ticker tape" },
            { 85, "Toggle Tsuruya's voice ticker tape" },
            { 86, "Toggle Taniguchi's voice ticker tape" },
            { 87, "Toggle Kunikida's voice ticker tape" },
            { 88, "Toggle Mysterious Girl's voice ticker tape" },
            { 89, "Reset settings to default ticker tape" },
            { 90, "All data will be erased prompt message box" },
            { 91, "Second prompt on data being erased" },
            { 92, "Data reset message box" },
            { 93, "All data erased message box" },
            { 94, "Batch Dialogue Display option" },
            { 95, "Dialogue Skipping setting" },
            { 96, "Puzzle Interrupt Scenes setting" },
            { 97, "Topic Stock Mode option" },
            { 98, "Batch Dialogue Display ticker tape" },
            { 99, "Dialogue Skipping ticker tape" },
            { 100, "Puzzle Interrupt Scenes ticker tape" },
            { 101, "Topic Stock Mode ticker tape" },
            { 102, "Batch Dialogue Display Off" },
            { 103, "Batch Dialogue Display On" },
            { 104, "Puzzle Interrupt Scenes Off" },
            { 105, "Puzzle Interrupt Scenes Unseen Only" },
            { 106, "Puzzle Interrupt Scenes On" },
            { 107, "Dialogue Skipping Fast Forward" },
            { 108, "Dialogue Skipping Skip Already Read" },
            { 109, "Kyon's Dialogue Box Group Selection" },
            { 111, "Group selection impossible selection made ticker tape" },
            { 112, "Kyon's Dialogue Box Companion Selection" },
            { 115, "Chess Mode Unlocked message box" },
            { 116, "Haruhi Suzumiya event unlocked message box" },
            { 117, "Mikuru Asahina event unlocked message box" },
            { 118, "Yuki Nagato event unlocked message box" },
            { 119, "Itsuki Koizumi event unlocked message box" },
            { 120, "Tsuruya event unlocked message box" },
            { 121, "Collected all Haruhi topics message box" },
            { 122, "Collected all Mikuru topics message box" },
            { 123, "Collected all Nagato topics message box" },
            { 124, "Collected all Koizumi topics message box" },
            { 125, "Collected all main topics message box" },
            { 126, "Cleared all chess puzzles message box" },
            { 127, "100%'d game message box" },
            { 128, "Extras unlocked message box" },
            { 129, "Mystery girl voice added to config message box" },
        };
    }
}
