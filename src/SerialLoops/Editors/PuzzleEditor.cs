using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System.Linq;

namespace SerialLoops.Editors
{
    public class PuzzleEditor : Editor
    {
        private PuzzleItem _puzzle;

        public PuzzleEditor(PuzzleItem item, ILogger log) : base(item, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            _puzzle = (PuzzleItem)Description;
            StackLayout mainLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
            };

            StackLayout panel1 = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
            };

            GroupBox topicsBox = new() { Text = "Associated Main Topics", Padding = 5 };
            StackLayout topics = new() { Orientation = Orientation.Horizontal, Spacing = 5 };
            foreach (var (topic, _) in _puzzle.Puzzle.AssociatedTopics.Take(_puzzle.Puzzle.AssociatedTopics.Count - 1))
            {
                topics.Items.Add(new LinkButton { Text = topic.ToString() });
            }
            topicsBox.Content = topics;
            panel1.Items.Add(topicsBox);

            GroupBox haruhiRoutesBox = new() { Text = "Haruhi Routes", Padding = 5 };
            StackLayout haruhiRoutes = new() { Orientation = Orientation.Vertical, Spacing = 8 };
            foreach (PuzzleHaruhiRoute haruhiRoute in _puzzle.Puzzle.HaruhiRoutes)
            {
                haruhiRoutes.Items.Add(haruhiRoute.ToString());
            }
            haruhiRoutesBox.Content = haruhiRoutes;
            panel1.Items.Add(haruhiRoutesBox);
            
            mainLayout.Items.Add(panel1);
            
            DropDown accompanyingCharacterDropdown = new();
            accompanyingCharacterDropdown.Items.AddRange(PuzzleItem.Characters.Select(c => new ListItem() { Text = c, Key = c }));
            accompanyingCharacterDropdown.SelectedIndex = PuzzleItem.Characters.IndexOf(_puzzle.Puzzle.Settings.AccompanyingCharacter);
            DropDown powerCharacter1Dropdown = new();
            powerCharacter1Dropdown.Items.AddRange(PuzzleItem.Characters.Select(c => new ListItem() { Text = c, Key = c }));
            powerCharacter1Dropdown.SelectedIndex = PuzzleItem.Characters.IndexOf(_puzzle.Puzzle.Settings.PowerCharacter1);
            DropDown powerCharacter2Dropdown = new();
            powerCharacter2Dropdown.Items.AddRange(PuzzleItem.Characters.Select(c => new ListItem() { Text = c, Key = c }));
            powerCharacter2Dropdown.SelectedIndex = PuzzleItem.Characters.IndexOf(_puzzle.Puzzle.Settings.PowerCharacter2);

            mainLayout.Items.Add(new StackLayout()
            {
                Spacing = 10,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    new GroupBox
                    {
                        Text = "Settings",
                        Padding = 5,
                        Content = new StackLayout
                        {
                            Spacing = 10,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            Items =
                            {
                                ControlGenerator.GetControlWithLabel("Map ID", new TextBox { Text = _puzzle.Puzzle.Settings.MapId.ToString() }),
                                ControlGenerator.GetControlWithLabel("Base Time", new TextBox { Text = _puzzle.Puzzle.Settings.BaseTime.ToString() }),
                                ControlGenerator.GetControlWithLabel("Number of Singularities", new TextBox { Text = _puzzle.Puzzle.Settings.NumSingularities.ToString() }),
                                ControlGenerator.GetControlWithLabel("Unknown 04", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown04.ToString() }),
                                ControlGenerator.GetControlWithLabel("Target Number", new TextBox { Text = _puzzle.Puzzle.Settings.TargetNumber.ToString() }),
                                ControlGenerator.GetControlWithLabel("Continue on Failure", new CheckBox { Checked = _puzzle.Puzzle.Settings.ContinueOnFailure }),
                                ControlGenerator.GetControlWithLabel("Accompanying Character", accompanyingCharacterDropdown),
                                ControlGenerator.GetControlWithLabel("Power Character 1", powerCharacter1Dropdown),
                                ControlGenerator.GetControlWithLabel("Power Character 2", powerCharacter2Dropdown),
                                ControlGenerator.GetControlWithLabel("Singularity", new SKGuiImage(_puzzle.SingularityImage)),
                                ControlGenerator.GetControlWithLabel("Topic Set", new TextBox { Text = _puzzle.Puzzle.Settings.TopicSet.ToString() }),
                                ControlGenerator.GetControlWithLabel("Unknown 15", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown15.ToString() }),
                                ControlGenerator.GetControlWithLabel("Unknown 16", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown16.ToString() }),
                                ControlGenerator.GetControlWithLabel("Unknown 17", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown17.ToString() }),
                            },
                        },
                    },
                },
            });

            return mainLayout;
        }
    }
}
