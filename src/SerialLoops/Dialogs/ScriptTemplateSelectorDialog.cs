using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Script;
using SerialLoops.Utility;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class ScriptTemplateSelectorDialog : Dialog<ScriptTemplate>
    {
        private readonly ILogger _log;
        private readonly Project _project;
        private readonly EventFile _script;
        private ScriptTemplates.TemplateOption _currentSelection;
        private TextBox _filter;
        private ListBox _selector;
        private Panel _description;

        public ScriptTemplateSelectorDialog(Project project, EventFile script, ILogger log)
        {
            _log = log;
            _project = project;
            _script = script;
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _filter.Focus();
        }

        private void InitializeComponent()
        {
            Title = "Select Template to Apply";
            MinimumSize = new Size(600, 350);
            Padding = 10;

            _filter = new TextBox
            {
                PlaceholderText = "Filter by name",
                Width = 200,
            };
            _filter.TextChanged += (sender, args) =>
            {
                _selector.DataStore = new ObservableCollection<ScriptTemplates.TemplateOption>(ScriptTemplates.AvailableTemplates
                    .Where(t => t.Name.Contains(_filter.Text, StringComparison.OrdinalIgnoreCase)));
            };

            _selector = new ListBox
            {
                Size = new Size(200, 330),
                DataStore = ScriptTemplates.AvailableTemplates,
                SelectedIndex = ScriptTemplates.AvailableTemplates.IndexOf(_currentSelection),
                ItemTextBinding = Binding.Delegate<ScriptTemplates.TemplateOption, string>(t => t.Name),
                ItemKeyBinding = Binding.Delegate<ScriptTemplates.TemplateOption, string>(t => t.Name),
            };

            _description = new StackLayout
            {
                MinimumSize = new(480, 330),
                Padding = 10,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items =
                {
                    ControlGenerator.GetControlWithIcon("Please select a template", "Template", _log)
                }
            };
            _selector.SelectedValueChanged += (sender, args) =>
            {
                _currentSelection = (ScriptTemplates.TemplateOption)_selector.SelectedValue;
                if (_currentSelection is not null)
                {
                    _description.Content = new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                        Items =
                        {
                            ControlGenerator.GetTextHeader(_currentSelection.Name),
                            _currentSelection.Description,
                        }
                    };
                } 
                else
                {
                    _description.Content = new StackLayout
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            ControlGenerator.GetControlWithIcon("Please select a template", "Template", _log)
                        }
                    };
                }
            };

            Button confirmButton = new() { Text = "Confirm" };
            confirmButton.Click += (sender, args) => Close(_currentSelection?.Template(_project, _script));
            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, args) => Close(null);

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new TableLayout(new TableRow
                    {
                        Cells =
                        {
                            new StackLayout
                            {
                                Orientation = Orientation.Vertical,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                Spacing = 10,
                                Items =
                                {
                                    _filter,
                                    _selector
                                }
                            },
                            _description
                        }
                    }),
                    new StackLayout
                    {
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Items =
                        {
                            confirmButton,
                            cancelButton
                        }
                    }
                }
            };
        }
    }
}
