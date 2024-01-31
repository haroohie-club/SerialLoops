using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System.IO;
using System.Linq;

namespace SerialLoops.Dialogs
{
    internal class EditTutorialMappingsDialog : FloatingForm
    {
        private readonly ILogger _log;
        private readonly Project _project;
        private readonly EditorTabsPanel _tabs;

        public EditTutorialMappingsDialog(Project project, EditorTabsPanel tabs, ILogger log)
        {
            _project = project;
            _tabs = tabs;
            _log = log;

            Title = "Edit Tutorial Mappings";
            MinimumSize = new(400, 600);
            Size = new(400, 600);
            Padding = 10;
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            StackLayout tutorialsLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
            };
            for (int i = 0; i < _project.TutorialFile.Tutorials.Count - 1; i++) // Minus one to avoid the padding
            {
                int currentIndex = i;

                Label idLabel = new() { Text = (_project.TutorialFile.Tutorials[currentIndex].Id - 1).ToString() };
                DropDown associatedScriptDropDown = new();
                associatedScriptDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(s => new ListItem { Key = s.DisplayName, Text = s.DisplayName }));
                ScriptItem associatedScript = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == _project.TutorialFile.Tutorials[currentIndex].AssociatedScript);
                associatedScriptDropDown.SelectedKey = associatedScript.DisplayName;
                StackLayout associatedLink = ControlGenerator.GetFileLink(associatedScript, _tabs, _log);

                StackLayout scriptLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 3,
                    Items =
                    {
                        ControlGenerator.GetControlWithLabel("Associated Script", associatedScriptDropDown),
                        associatedLink,
                    }
                };

                associatedScriptDropDown.SelectedKeyChanged += (sender, args) =>
                {
                    ScriptItem newAssociatedScript = (ScriptItem)_project.Items.First(i => i.DisplayName == associatedScriptDropDown.SelectedKey);
                    _project.TutorialFile.Tutorials[currentIndex].AssociatedScript = (short)newAssociatedScript.Event.Index;
                    scriptLayout.Items.RemoveAt(1);
                    scriptLayout.Items.Add(ControlGenerator.GetFileLink(newAssociatedScript, _tabs, _log));
                };

                tutorialsLayout.Items.Add(new GroupBox
                {
                    Text = $"Tutorial {idLabel.Text}",
                    Content = new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 5,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("ID", idLabel),
                            scriptLayout,
                        }
                    }
                });
            }

            Button saveButton = new() { Text = "Save" };
            saveButton.Click += (sender, args) =>
            {
                _log.Log("Attempting to save tutorial mappings...");
                Lib.IO.WriteStringFile(Path.Combine("assets", "events", $"{_project.TutorialFile.Index:X3}.s"), _project.TutorialFile.GetSource([]), _project, _log);
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
                    new(new Scrollable { Content = tutorialsLayout, Height = 500 }),
                    new(buttonsLayout),
                },
            };
        }
    }
}
