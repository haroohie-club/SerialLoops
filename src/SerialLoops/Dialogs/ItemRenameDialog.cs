using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class ItemRenameDialog : Dialog
    {
        private ItemDescription _item;
        private Project _project;
        private ILogger _log;

        public ItemRenameDialog(ItemDescription item, Project project, ILogger log)
        {
            _item = item;
            _project = project;
            _log = log;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            MinimumSize = new(256, 100);

            TextBox nameBox = new();

            Button renameButton = new() { Text = "Rename" };
            Button cancelButton = new() { Text = "Cancel" };
            renameButton.Click += (sender, args) =>
            {
                string name = nameBox.Text.Trim();
                if (_project.Items.All(i => !i.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    _item.Rename(name);
                    Close();
                }
                else
                {
                    _log.LogError($"Name '{name}' is already in use!");
                }
            };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 3,
                Padding = 5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    new Label { Text = "New Name" },
                    new StackLayoutItem { Control = nameBox },
                    new StackLayoutItem
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Control = new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 3,
                            Items =
                            {
                                renameButton,
                                cancelButton,
                            },
                        },
                    },
                },
            };
        }
    }
}
