using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using System.Linq;

namespace SerialLoops.Editors
{
    public class PuzzleEditor : Editor
    {
        private PuzzleItem _puzzle;

        public PuzzleEditor(PuzzleItem item, Project project, EditorTabsPanel tabs, ILogger log) : base(item, log, project, tabs)
        {
        }

        public override Container GetEditorPanel()
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

            MapItem map = (MapItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Map && i.Name ==
                _project.Dat.Files.First(f => f.Name == "QMAPS").CastTo<QMapFile>().QMaps[_puzzle.Puzzle.Settings.MapId].Name[0..^2]);

            GroupBox topicsBox = new() { Text = "Associated Main Topics", Padding = 5 };
            StackLayout topics = new() { Orientation = Orientation.Vertical, Spacing = 5 };
            foreach (var (topicId, _) in _puzzle.Puzzle.AssociatedTopics.Take(_puzzle.Puzzle.AssociatedTopics.Count - 1))
            {
                TopicItem topic = (TopicItem)_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).Topic.Id == topicId);
                LinkButton topicButton = new() { Text = topic?.Topic.Title.GetSubstitutedString(_project) ?? topicId.ToString() };
                if (topic is not null)
                {
                    topicButton.Click += (s, e) => _tabs.OpenTab(topic, _log);
                }
                topics.Items.Add(topicButton);
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
            
            DropDown accompanyingCharacterDropdown = new() { Enabled = false };
            accompanyingCharacterDropdown.Items.AddRange(PuzzleItem.Characters.Select(c => new ListItem() { Text = c, Key = c }));
            accompanyingCharacterDropdown.SelectedIndex = PuzzleItem.Characters.IndexOf(_puzzle.Puzzle.Settings.AccompanyingCharacter);
            DropDown powerCharacter1Dropdown = new() { Enabled = false };
            powerCharacter1Dropdown.Items.AddRange(PuzzleItem.Characters.Select(c => new ListItem() { Text = c, Key = c }));
            powerCharacter1Dropdown.SelectedIndex = PuzzleItem.Characters.IndexOf(_puzzle.Puzzle.Settings.PowerCharacter1);
            DropDown powerCharacter2Dropdown = new() { Enabled = false };
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
                                ControlGenerator.GetControlWithLabel("Map", ControlGenerator.GetFileLink(map, _tabs, _log)),
                                ControlGenerator.GetControlWithLabel("Base Time", new TextBox { Text = _puzzle.Puzzle.Settings.BaseTime.ToString(), Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Number of Singularities", new TextBox { Text = _puzzle.Puzzle.Settings.NumSingularities.ToString(), Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Unknown 04", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown04.ToString(), Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Target Number", new TextBox { Text = _puzzle.Puzzle.Settings.TargetNumber.ToString(), Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Continue on Failure", new CheckBox { Checked = _puzzle.Puzzle.Settings.ContinueOnFailure, Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Accompanying Character", accompanyingCharacterDropdown),
                                ControlGenerator.GetControlWithLabel("Power Character 1", powerCharacter1Dropdown),
                                ControlGenerator.GetControlWithLabel("Power Character 2", powerCharacter2Dropdown),
                                ControlGenerator.GetControlWithLabel("Singularity", new SKGuiImage(_puzzle.SingularityImage)),
                                ControlGenerator.GetControlWithLabel("Topic Set", new TextBox { Text = _puzzle.Puzzle.Settings.TopicSet.ToString(), Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Unknown 15", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown15.ToString(), Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Unknown 16", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown16.ToString(), Enabled = false }),
                                ControlGenerator.GetControlWithLabel("Unknown 17", new TextBox { Text = _puzzle.Puzzle.Settings.Unknown17.ToString(), Enabled = false }),
                            },
                        },
                    },
                },
            });

            return mainLayout;
        }
    }
}
