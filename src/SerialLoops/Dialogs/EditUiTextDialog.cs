using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Util;
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
                TextBox uiTextBox = new() { Width = 400 };
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

                GroupBox uiTextGroup = new()
                {
                    Content = uiTextBox,
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

            uiTextLayout.Items.Add(new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items =
                {
                    saveButton,
                    cancelButton,
                }
            });

            Content = new Scrollable { Content = uiTextLayout };
        }
    }
}
