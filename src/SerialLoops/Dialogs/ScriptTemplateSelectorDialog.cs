using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class ScriptTemplateSelectorDialog : Dialog<ScriptTemplate>
    {
        private readonly ILogger _log;
        private readonly Project _project;
        private ScriptTemplates.TemplateOption _currentSelection;
        private TextBox _filter;
        private ListBox _selector;
        private Panel _description;

        public ScriptTemplateSelectorDialog(Project project, ILogger log)
        {
            _log = log;
            _project = project;
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
            MinimumSize = new Size(450, 400);
            Padding = 10;

            _filter = new TextBox
            {
                PlaceholderText = "Filter by name",
                Width = 150,
            };
            _filter.TextChanged += (sender, args) =>
            {
                _selector.DataStore = new ObservableCollection<ScriptTemplates.TemplateOption>(ScriptTemplates.AvailableTemplates
                    .Where(t => t.Name.Contains(_filter.Text, StringComparison.OrdinalIgnoreCase)));
            };

            _selector = new ListBox
            {
                Size = new Size(150, 390),
                DataStore = ScriptTemplates.AvailableTemplates,
                SelectedIndex = ScriptTemplates.AvailableTemplates.IndexOf(_currentSelection),
                ItemTextBinding = Binding.Delegate<ScriptTemplates.TemplateOption, string>(t => t.Name),
                ItemKeyBinding = Binding.Delegate<ScriptTemplates.TemplateOption, string>(t => t.Name),
            };

            _description = new StackLayout
            {
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = 10
            };
            _selector.SelectedValueChanged += (sender, args) =>
            {
                _currentSelection = (ScriptTemplates.TemplateOption)_selector.SelectedValue;
                _description.Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Spacing = 10,
                    Items =
                    {
                        _currentSelection.Name,
                        _currentSelection.Description,
                    }
                };
            };

            Button closeButton = new() { Text = "Confirm" };
            closeButton.Click += (sender, args) => Close(_currentSelection?.Template(_project));
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
                            closeButton,
                            cancelButton
                        }
                    }
                }
            };
        }
    }
}
