using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Script;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;

namespace SerialLoops.Dialogs
{
    public class ScriptTemplateCreationDialog : Dialog<ScriptTemplate>
    {
        private readonly ILogger _log;
        private readonly Project _project;
        private readonly Dictionary<ScriptSection, List<ScriptItemCommand>> _commands;
        private TextBox _templateNameBox;

        public ScriptTemplateCreationDialog(Project project, Dictionary<ScriptSection, List<ScriptItemCommand>> commands, ILogger log)
        {
            _project = project;
            _commands = commands;
            _log = log;
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _templateNameBox.Focus();
        }

        public void InitializeComponent()
        {
            Title = Application.Instance.Localize(this, "Template Properties");
            MinimumSize = new Size(400, 250);
            Padding = 10;

            _templateNameBox = new()
            {
                PlaceholderText = Application.Instance.Localize(this, "Template Name"),
                Width = 300
            };

            TextArea templateDescBox = new()
            {
                Width = 300,
                Height = 75,
            };

            Button confirmButton = new() { Text = Application.Instance.Localize(this, "Confirm") };
            confirmButton.Click += (sender, args) =>
            {
                try
                {
                    ScriptTemplate template = new(_templateNameBox.Text, templateDescBox.Text, _commands, _project);
                    Close(template);
                }
                catch (Exception ex)
                {
                    _log.LogException(Application.Instance.Localize(this, "Failed to generate script template!"), ex);
                    Close(null);
                }
            };
            Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
            cancelButton.Click += (sender, args) => Close(null);

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Padding = 10,
                Spacing = 10,
                Items =
                {
                    new TableLayout(
                        new TableRow("Name", _templateNameBox),
                        new TableRow("Description", templateDescBox)
                        )
                    {
                        Spacing = new Size(5, 10),
                    },
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
                    },
                },
            };
        }
    }
}
