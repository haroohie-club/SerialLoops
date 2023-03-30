using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using Microsoft.VisualBasic;
using SerialLoops.Lib;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using static SerialLoops.Dialogs.PreferencesDialog;

namespace SerialLoops.Dialogs
{
    public partial class PreferencesDialog : Dialog
    {
        public Config Configuration { get; set; }
        private ILogger _log { get; set; }

        public PreferencesDialog(Config config, ILogger log)
        {
            Title = "Preferences";
            MinimumSize = new Size(450, 300);
            Configuration = config;
            _log = log;

            Button saveButton = new() { Text = "Save" };
            saveButton.Click += SaveButton_Click;

            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, args) => Close();

            Content = new TableLayout(
                new TableRow(InitializeOptions()),
                new TableRow(
                    new StackLayout
                    {
                        Padding = 10,
                        Spacing = 10,
                        Orientation = Orientation.Horizontal,
                        VerticalContentAlignment = VerticalAlignment.Bottom,
                        Items =
                        {
                            saveButton,
                            cancelButton
                        }
                    })
                );
        }

        private StackLayout InitializeOptions()
        {
            return new()
            {
                Padding = 10,
                Spacing = 10,
                Orientation = Orientation.Vertical,
                Items =
                {
                    new OptionsGroup
                    (
                        "Build",
                        new()
                        {
                            new FolderOption
                            {
                                Name = "DevkitARM Path",
                                Path = Configuration.DevkitArmPath,
                                OnChange = (path) => Configuration.DevkitArmPath = path
                            },
                            new FileOption
                            {
                                Name = "Emulator Path",
                                Path = Configuration.EmulatorPath,
                                OnChange = (path) => Configuration.EmulatorPath = path
                            }
                        }
                    ),
                    new OptionsGroup(
                        "Projects",
                        new()
                        {
                            new BooleanOption
                            {
                                Name = "Auto Re-Open Last Project",
                                Value = Configuration.AutoReopenLastProject,
                                OnChange = (value) => Configuration.AutoReopenLastProject = value
                            },
                            new BooleanOption
                            {
                                Name = "Remember Project Workspace",
                                Value = Configuration.RememberProjectWorkspace,
                                OnChange = (value) => Configuration.RememberProjectWorkspace = value
                            },
                            new BooleanOption
                            {
                                Name = "Remove Missing Projects",
                                Value = Configuration.RemoveMissingProjects,
                                OnChange = (value) => Configuration.RemoveMissingProjects = value
                            }
                        }
                    )
                }
            };
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Configuration.Save(_log);
            Close();
        }

        internal class OptionsGroup : GroupBox
        {
            public OptionsGroup(string name, List<Option> options)
            {
                Text = name;
                Width = 450;
                Padding = 10;
                Content = new TableLayout(options.Select(option => option.GetOptionRow()));
            }
        }

        internal abstract class Option
        {
            public string Name { get; set; }

            public abstract Control GetControl();

            public TableRow GetOptionRow()
            {
                return new TableRow(new StackLayout() 
                { 
                    Items = { Name }, 
                    Orientation = Orientation.Horizontal, 
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                }, GetControl());
            }
        }

        internal class FileOption : Option
        {
            public Action<string> OnChange { get; set; }
            public string Path 
            { 
                get => _pathBox.Text;
                set { _pathBox.Text = value; }
            }

            protected TextBox _pathBox;
            private readonly Button _pickerButton;
            
            public FileOption()
            {
                _pathBox = new TextBox() { Text = "", Width = 225 };
                _pathBox.TextChanged += (sender, args) => { OnChange?.Invoke(Path); };

                _pickerButton = new Button() { Text = "Select..." };
                _pickerButton.Click += SelectButton_OnClick;
            }

            public override Control GetControl()
            {
                return new StackLayout
                {
                    Padding = 5,
                    Spacing = 5,
                    Orientation = Orientation.Horizontal,
                    Items = { _pathBox, _pickerButton }
                };
            }

            protected virtual void SelectButton_OnClick(object sender, EventArgs e)
            {
                OpenFileDialog openFileDialog = new();
                if (openFileDialog.ShowAndReportIfFileSelected(GetControl()))
                {
                    _pathBox.Text = openFileDialog.FileName;
                }
            }
        }

        internal class FolderOption : FileOption
        {
            protected override void SelectButton_OnClick(object sender, EventArgs e)
            {
                SelectFolderDialog selectFolderDialog = new();
                if (selectFolderDialog.ShowAndReportIfFileSelected(GetControl()))
                {
                    _pathBox.Text = selectFolderDialog.Directory;
                }
            }
        }

        internal class BooleanOption : Option
        {
            public Action<bool> OnChange { get; set; }
            public bool Value
            {
                get => _checkBox.Checked is true;
                set { _checkBox.Checked = value; }
            }

            private readonly CheckBox _checkBox;

            public BooleanOption()
            {
                _checkBox = new CheckBox() { Checked = false };
                _checkBox.CheckedChanged += (sender, e) => OnChange?.Invoke(Value);
            }

            public override Control GetControl()
            {
                return new StackLayout
                {
                    Padding = 5,
                    Items = { _checkBox }
                };
            }
        }

    }
}
