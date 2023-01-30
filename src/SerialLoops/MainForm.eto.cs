using Eto.Drawing;
using Eto.Forms;
using System;

namespace SerialLoops
{
    partial class MainForm : Form
    {
        void InitializeComponent()
        {
            Title = "Serial Loops";
            ClientSize = new(769, 420);
            MinimumSize = new(769, 420);
            Padding = 10;

            Content = new StackLayout
            {
                Items =
                {
                    "Hello World!",
					// add more controls here
				}
            };

            // create a few commands that can be used for the menu and toolbar
            Command clickMe = new() { MenuText = "Click Me!", ToolBarText = "Click Me!" };
            clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

            Command aboutCommand = new() { MenuText = "About..." };
            AboutDialog aboutDialog = new() { ProgramName = "Serial Loops", Developers = new string[] { "Jonko", "William" }, Copyright = "© Haroohie Translation Club, 2023", Website = new Uri("https://haroohie.club")  };
            aboutCommand.Executed += (sender, e) => aboutDialog.ShowDialog(this);

            // create menu
            Menu = new MenuBar
            {
                Items =
                {
					// File submenu
					new SubMenuItem { Text = "&File", Items = { clickMe } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
                ApplicationItems =
                {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
                },
                AboutItem = aboutCommand
            };

            // create toolbar			
            ToolBar = new ToolBar { Items = { clickMe } };
        }
    }
}
