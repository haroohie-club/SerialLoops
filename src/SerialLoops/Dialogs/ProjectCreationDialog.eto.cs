using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Logging;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SerialLoops
{
    partial class ProjectCreationDialog : Dialog
    {
        public LoopyLogger Log;
        public Config Config;
        public Project NewProject { get; private set; }

        private TextBox _nameBox;
        private DropDown _languageDropDown;
        private Label _romPath;

        public const string NO_ROM_TEXT = "None Selected";
        private static readonly Regex ALLOWED_CHARACTERS_REGEX = new(@"[A-z\d-_\.]");

        void InitializeComponent()
        {
            Title = Application.Instance.Localize(this, "Create New Project");
            MinimumSize = new Size(400, 275);
            Padding = 10;

            _nameBox = new()
            {
                PlaceholderText = Application.Instance.Localize(this, "Haroohie"),
                Size = new Size(150, 25),
            };
            _nameBox.TextChanging += (sender, args) =>
            {
                if (args.NewText.Any(c => !ALLOWED_CHARACTERS_REGEX.IsMatch(c.ToString())))
                {
                    args.Cancel = true;
                }
            };
            _languageDropDown = new();
            _languageDropDown.Items.AddRange(_availableLanguages.Select(a => new ListItem() { Text = a.Key, Key = a.Value }));
            _languageDropDown.SelectedIndex = 0;
            _romPath = new() { Text = Application.Instance.Localize(this, NO_ROM_TEXT) };
            Command pickRomCommand = new();
            pickRomCommand.Executed += PickRomCommand_Executed;
            Command createCommand = new();
            createCommand.Executed += CreateCommand_Executed;
            Command cancelCommand = new();
            cancelCommand.Executed += CancelCommand_Executed;

            Content = new StackLayout
            {
                Spacing = 10,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    new GroupBox
                    {
                        Text = Application.Instance.Localize(this, "Project Options"),
                        Padding = 5,
                        MinimumSize = new Size(300, 100),
                        Content = new StackLayout
                        {
                            Spacing = 10,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Items =
                            {
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                    Spacing = 10,
                                    Items =
                                    {
                                        Application.Instance.Localize(this, "Name"),
                                        _nameBox,
                                    }
                                },
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                    Spacing = 10,
                                    Items =
                                    {
                                        Application.Instance.Localize(this, "Language Template"),
                                        _languageDropDown,
                                    }
                                }
                            }
                        }
                    },
                    new GroupBox
                    {
                        Text = Application.Instance.Localize(this, "Select ROM"),
                        Padding = 5,
                        MinimumSize = new Size(300, 80),
                        Content = new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 10,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Items =
                            {
                                new Button { Text = Application.Instance.Localize(this, "Open ROM"), Command = pickRomCommand },
                                _romPath,
                            }
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Padding = 5,
                        Spacing = 10,
                        Items =
                        {
                            new Button { Text = Application.Instance.Localize(this, "Create"), Command = createCommand },
                            new Button { Text = Application.Instance.Localize(this, "Cancel"), Command = cancelCommand }
                        }
                    },
                }
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            if (Log is null)
            {
                // We can't log that log is null, so we have to throw
                throw new LoggerNullException();
            }
            if (Config is null)
            {
                Log.LogError($"Config not provided to project creation dialog");
                Close();
            }
            base.OnLoad(e);
        }
        
        private void PickRomCommand_Executed(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Title = Application.Instance.Localize(this, "Open ROM"), CheckFileExists = true };
            openFileDialog.Filters.Add(new(Application.Instance.Localize(this, "Chokuretsu ROM"), ".nds"));
            if (openFileDialog.ShowAndReportIfFileSelected(this))
            {
                _romPath.Text = openFileDialog.FileName;
            }
        }

        private void CancelCommand_Executed(object sender, EventArgs e)
        {
            Close();
        }

        private void CreateCommand_Executed(object sender, EventArgs e)
        {
            if (_romPath.Text == NO_ROM_TEXT)
            {
                MessageBox.Show(Application.Instance.Localize(this, "Please select a ROM before creating the project."), Application.Instance.Localize(this, "Project Creation Warning"), MessageBoxType.Warning);
            }
            else if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageBox.Show(Application.Instance.Localize(this, "Please choose a project name before creating the project."), Application.Instance.Localize(this, "Project Creation Warning"), MessageBoxType.Warning);
            }
            else
            {
                NewProject = new(_nameBox.Text, _languageDropDown.Items[_languageDropDown.SelectedIndex].Key, Config, s => Application.Instance.Localize(null, s), Log);
                string romPath = _romPath.Text;
                NewProject.SetBaseRomHash(romPath);
                LoopyProgressTracker tracker = new(s => Application.Instance.Localize(null, s));
                ProgressDialog _ = new(() => 
                {
                    ((IProgressTracker)tracker).Focus(Application.Instance.Localize(this, "Creating Project"), 1);
                    IO.OpenRom(NewProject, romPath, Log, tracker);
                    tracker.Finished++;
                    NewProject.Load(Config, Log, tracker);
                }, Close, tracker, Application.Instance.Localize(this, "Creating Project"));
            }
        }

        private readonly static Dictionary<string, string> _availableLanguages = new()
        {
            { Application.Instance.Localize(null, "English"), "en" },
            { Application.Instance.Localize(null, "Japanese"), "ja" },
            { Application.Instance.Localize(null, "Chinese (Simplified)"), "zh-Hans" },
            { Application.Instance.Localize(null, "Russian"), "ru" },
            { Application.Instance.Localize(null, "Spanish"), "es" },
            { Application.Instance.Localize(null, "Portuguese (Brazilian)"), "pt-BR" },
            { Application.Instance.Localize(null, "Italian"), "it" },
            { Application.Instance.Localize(null, "French"), "fr" },
            { Application.Instance.Localize(null, "German"), "de" },
            { Application.Instance.Localize(null, "Greek"), "el" },
        };
    }
}
