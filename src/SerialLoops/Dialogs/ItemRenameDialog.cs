using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
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

            string prefix = _item.Type switch
            {
                ItemDescription.ItemType.Background => "BG_",
                ItemDescription.ItemType.BGM => "BGM_",
                ItemDescription.ItemType.Character_Sprite => "SPR_",
                ItemDescription.ItemType.Chess => "CHESS_",
                ItemDescription.ItemType.Chibi => "CHB_",
                ItemDescription.ItemType.Character => "CHR_",
                ItemDescription.ItemType.Group_Selection => "GRP_",
                ItemDescription.ItemType.Map => "MAP_",
                ItemDescription.ItemType.Place => "PLC_",
                ItemDescription.ItemType.Puzzle => "PZL_",
                ItemDescription.ItemType.SFX => "SFX_",
                ItemDescription.ItemType.Transition => "TRN_",
                _ => "",
            };

            TextBox nameBox = new();
            if (_item.DisplayName.StartsWith(prefix))
            {
                nameBox.Text = _item.DisplayName[prefix.Length..];
            }
            else
            {
                nameBox.Text = _item.DisplayName;
            }
            nameBox.SelectAll();

            Button renameButton = new() { Text = "Rename" };
            Button cancelButton = new() { Text = "Cancel" };

            void renameItem()
            {
                string name = $"{prefix}{nameBox.Text.Trim()}";
                if (_project.Items.All(i => !i.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    _item.Rename(name);
                    Close();
                }
                else
                {
                    _log.LogError($"Name '{name}' is already in use!");
                }
            }
            renameButton.Click += (sender, args) =>
            {
                renameItem();
            };
            nameBox.KeyUp += (sender, args) =>
            {
                if (args.KeyData == Keys.Enter)
                {
                    renameItem();
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
                    ControlGenerator.GetControlWithLabel(prefix, nameBox),
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
