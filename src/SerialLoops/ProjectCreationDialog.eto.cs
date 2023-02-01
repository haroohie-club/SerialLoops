using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Lib;
using SerialLoops.Lib.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private const string NO_ROM_TEXT = "None Selected";
        private const string ASSETS_URL = "https://github.com/haroohie-club/ChokuretsuTranslationAssets/archive/refs/heads/main.zip";
        private const string STRINGS_URL = "https://github.com/haroohie-club/ChokuretsuTranslationStrings/archive/refs/heads/main.zip";

        void InitializeComponent()
        {
            Title = "Create New Project";
            MinimumSize = new Size(400, 300);
            Padding = 10;

            _nameBox = new();
            _languageDropDown = new();
            _languageDropDown.Items.AddRange(_availableLanguages.Select(a => new ListItem() { Text = a.Key, Key = a.Value }));
            _romPath = new() { Text = NO_ROM_TEXT };
            Command pickRomCommand = new();
            pickRomCommand.Executed += PickRomCommand_Executed;
            Command createCommand = new();
            createCommand.Executed += CreateCommand_Executed;

            Content = new StackLayout
            {
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Items =
                        {
                            "Name: ",
                            _nameBox,
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Items =
                        {
                            "Language: ",
                            _languageDropDown,
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Items =
                        {
                            new Button { Text = "Open ROM", Command = pickRomCommand },
                            _romPath,
                        }
                    },
                    new Button { Text = "Create", Command = createCommand },
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
            OpenFileDialog openFileDialog = new() { Title = "Open ROM", CheckFileExists = true };
            openFileDialog.Filters.Add(new("Chokuretsu ROM", ".nds"));
            if (openFileDialog.ShowDialog(this) == DialogResult.Ok)
            {
                _romPath.Text = openFileDialog.FileName;
            }
        }

        private void CreateCommand_Executed(object sender, EventArgs e)
        {
            if (_romPath.Text == NO_ROM_TEXT)
            {
                MessageBox.Show("Please select a ROM before creating the project.");
            }
            else if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageBox.Show("Please choose a project name before creating the project.");
            }
            else
            {
                NewProject = new(_nameBox.Text, _languageDropDown.Items[_languageDropDown.SelectedIndex].Key, Config, Log);
                IO.OpenRom(NewProject, _romPath.Text);
                if (NewProject.LangCode != "ja")
                {
                    if (MessageBox.Show("Would you like to download assets/strings from GitHub?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) == DialogResult.Yes)
                    {
                        MessageBox.Show("Assets & strings are not currently publically available. Please join the Haroohie Translation Club Discord server to request an asset bundle.");
                        // IO.FetchAssets(NewProject, new(ASSETS_URL), new(STRINGS_URL), Log);
                    }
                    IO.SetUpLocalizedHacks(NewProject);
                }
                NewProject.LoadArchives(Log);
                Close();
            }
        }

        private readonly static Dictionary<string, string> _availableLanguages = new()
        {
            { "English", "en" },
            { "Japanese", "ja" },
            { "Russian", "ru" },
            { "Spanish", "es" },
            { "Portuguese (Brazilian)", "pt-BR" },
            { "Italian", "it" },
            { "French", "fr" },
            { "German", "de" },
            { "Greek", "el" },
        };
    }
}
