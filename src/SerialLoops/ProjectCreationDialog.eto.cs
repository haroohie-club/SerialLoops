using System;
using Eto.Forms;
using Eto.Drawing;

namespace SerialLoops
{
	partial class ProjectCreationDialog : Dialog
    {
        public Config Config;
        public Project NewProject { get; private set; }

        private TextBox _nameBox;
		private Label _romPath;

		private const string NO_ROM_TEXT = "None Selected";

        void InitializeComponent()
		{
			Title = "Create New Project";
			MinimumSize = new Size(400, 300);
			Padding = 10;

            _nameBox = new();
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
            if (Config is null)
            {
                // TODO: Log error saying config must be provided
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
                NewProject = new(_nameBox.Text, Config);
                IO.OpenRom(NewProject, _romPath.Text);
                Close();
            }
        }
    }
}
