using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using System.Linq;

namespace SerialLoops.Editors
{
    public class TopicEditor : Editor
    {
        private TopicItem _topic;

        private NumericStepper _baseTimeStepper;
        private NumericStepper _kyonTimeStepper;
        private Label _kyonTimeLabel;
        private NumericStepper _mikuruTimeStepper;
        private Label _mikuruTimeLabel;
        private NumericStepper _nagatoTimeStepper;
        private Label _nagatoTimeLabel;
        private NumericStepper _koizumiTimeStepper;
        private Label _koizumiTimeLabel;

        public TopicEditor(TopicItem topic, Project project, ILogger log) : base(topic, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _topic = (TopicItem)Description;

            TableLayout idLayout = new()
            {
                Spacing = new(3, 3),
                Rows =
                {
                    ControlGenerator.GetControlWithLabelTable("ID", new Label { Text = _topic.Topic.Id.ToString() }),
                }
            };
            if (_topic.HiddenMainTopic is not null)
            {
                idLayout.Rows.Add(ControlGenerator.GetControlWithLabelTable("Hidden ID", new Label { Text = _topic.HiddenMainTopic.Id.ToString() }));
            }

            TextBox titleTextBox = new() { Text = _topic.Topic.Title.GetSubstitutedString(_project), Width = 300 };
            titleTextBox.TextChanged += (sender, args) =>
            {
                _topic.Topic.Title = titleTextBox.Text.GetOriginalString(_project);
                _topic.Rename($"{_topic.Topic.Id} - {titleTextBox.Text}");
                UpdateTabTitle(false, titleTextBox);
            };

            DropDown linkedScriptDropDown = new();
            linkedScriptDropDown.Items.Add(new ListItem { Key = "NONE", Text = "NONE" });
            linkedScriptDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(s => new ListItem { Key = s.Name, Text = s.Name, Tag = (short)((ScriptItem)s).Event.Index }));
            linkedScriptDropDown.SelectedKey = GetAssociatedScript()?.Name ?? "NONE";

            linkedScriptDropDown.SelectedKeyChanged += (sender, args) =>
            {
                if (linkedScriptDropDown.SelectedKey == "NONE")
                {
                    if (_topic.HiddenMainTopic is not null)
                    {
                        _topic.HiddenMainTopic.EventIndex = 0;
                    }
                    else
                    {
                        _topic.Topic.EventIndex = 0;
                    }
                }
                else
                {
                    if (_topic.HiddenMainTopic is not null)
                    {
                        _topic.HiddenMainTopic.EventIndex = (short)((ListItem)linkedScriptDropDown.Items[linkedScriptDropDown.SelectedIndex]).Tag;
                    }
                    else
                    {
                        _topic.Topic.EventIndex = (short)((ListItem)linkedScriptDropDown.Items[linkedScriptDropDown.SelectedIndex]).Tag;
                    }
                }
                UpdateTabTitle(false);
            };

            _baseTimeStepper = new() { Value = _topic.Topic.BaseTimeGain, MaxValue = short.MaxValue, MinValue = 0, MaximumDecimalPlaces = 0 };
            _baseTimeStepper.ValueChanged += (sender, args) =>
            {
                _topic.Topic.BaseTimeGain = (short)_baseTimeStepper.Value;

                UpdateKyonTime();
                UpdateMikuruTime();
                UpdateNagatoTime();
                UpdateKoizumiTime();
                UpdateTabTitle(false);
            };

            _kyonTimeStepper = new() { Value = _topic.Topic.KyonTimePercentage, MaxValue = short.MaxValue, MinValue = 0, MaximumDecimalPlaces = 0 };
            _kyonTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.KyonTimePercentage / 100.0)).ToString() };
            _kyonTimeStepper.ValueChanged += (sender, args) =>
            {
                _topic.Topic.KyonTimePercentage = (short)_kyonTimeStepper.Value;

                UpdateKyonTime();
                UpdateTabTitle(false);
            };

            _mikuruTimeStepper = new() { Value = _topic.Topic.MikuruTimePercentage, MaxValue = short.MaxValue, MinValue = 0, MaximumDecimalPlaces = 0 };
            _mikuruTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.MikuruTimePercentage / 100.0)).ToString() };
            _mikuruTimeStepper.ValueChanged += (sender, args) =>
            {
                _topic.Topic.MikuruTimePercentage = (short)_mikuruTimeStepper.Value;

                UpdateMikuruTime();
                UpdateTabTitle(false);
            };

            _nagatoTimeStepper = new() { Value = _topic.Topic.NagatoTimePercentage, MaxValue = short.MaxValue, MinValue = 0, MaximumDecimalPlaces = 0 };
            _nagatoTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.NagatoTimePercentage / 100.0)).ToString() };
            _nagatoTimeStepper.ValueChanged += (sender, args) =>
            {
                _topic.Topic.NagatoTimePercentage = (short)_nagatoTimeStepper.Value;

                UpdateNagatoTime();
                UpdateTabTitle(false);
            };

            _koizumiTimeStepper = new() { Value = _topic.Topic.KoizumiTimePercentage, MaxValue = short.MaxValue, MinValue = 0, MaximumDecimalPlaces = 0 };
            _koizumiTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.KoizumiTimePercentage / 100.0)).ToString() };
            _koizumiTimeStepper.ValueChanged += (sender, args) =>
            {
                _topic.Topic.KoizumiTimePercentage = (short)_koizumiTimeStepper.Value;

                UpdateKoizumiTime();
                UpdateTabTitle(false);
            };

            StackLayout timesLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Base Time Gain", ControlGenerator.GetControlWithSuffix(_baseTimeStepper, "sec")),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("Kyon Time Percentage", ControlGenerator.GetControlWithSuffix(_kyonTimeStepper, "%")),
                            ControlGenerator.GetControlWithSuffix(_kyonTimeLabel, "sec"),
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("Mikuru Time Percentage", ControlGenerator.GetControlWithSuffix(_mikuruTimeStepper, "%")),
                            ControlGenerator.GetControlWithSuffix(_mikuruTimeLabel, "sec"),
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("Nagato Time Percentage", ControlGenerator.GetControlWithSuffix(_nagatoTimeStepper, "%")),
                            ControlGenerator.GetControlWithSuffix(_nagatoTimeLabel, "sec"),
                        }
                    },
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("Koizumi Time Percentage", ControlGenerator.GetControlWithSuffix(_koizumiTimeStepper, "%")),
                            ControlGenerator.GetControlWithSuffix(_koizumiTimeLabel, "sec"),
                        }
                    },
                }
            };

            DropDown episodeGroupDropDown = new()
            {
                Items =
                {
                    new ListItem { Key = "1", Text = "Episode 1" },
                    new ListItem { Key = "2", Text = "Episode 2" },
                    new ListItem { Key = "3", Text = "Episode 3" },
                    new ListItem { Key = "4", Text = "Episode 4" },
                    new ListItem { Key = "5", Text = "Episode 5" },
                },
                SelectedKey = _topic.Topic.EpisodeGroup.ToString(),
            };
            episodeGroupDropDown.SelectedKeyChanged += (sender, args) =>
            {
                _topic.Topic.EpisodeGroup = byte.Parse(episodeGroupDropDown.SelectedKey);
                UpdateTabTitle(false);
            };

            NumericStepper puzzlePhaseGroupStepper = new()
            {
                Value = _topic.Topic.GroupSelection,
                DecimalPlaces = 0,
                MinValue = 0,
            };
            puzzlePhaseGroupStepper.ValueChanged += (sender, args) =>
            {
                _topic.Topic.GroupSelection = (byte)puzzlePhaseGroupStepper.Value;
                UpdateTabTitle(false);
            };

            StackLayout groupsLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Episode Group", episodeGroupDropDown),
                    ControlGenerator.GetControlWithLabel("Puzzle Phase Group", puzzlePhaseGroupStepper),
                },
            };

            //StackLayout unknownsLayout = new()
            //{
            //    Orientation = Orientation.Vertical,
            //    Spacing = 5,
            //    Items =
            //    {
            //        ControlGenerator.GetControlWithLabel("Unknown 03", new TextBox { Text = _topic.Topic.UnknownShort03.ToString() }),
            //        ControlGenerator.GetControlWithLabel("Unknown 04", new TextBox { Text = _topic.Topic.UnknownShort04.ToString() }),
            //    }
            //};

            return new TableLayout(idLayout,
                ControlGenerator.GetControlWithLabel("Title", titleTextBox),
                ControlGenerator.GetControlWithLabel("Type", _topic.Topic.CardType.ToString()),
                ControlGenerator.GetControlWithLabel("Associated Script", linkedScriptDropDown),
                groupsLayout,
                new GroupBox() { Text = "Times", Content = timesLayout });
        }

        private ItemDescription GetAssociatedScript()
        {
            ItemDescription associatedScript;
            if (_topic.HiddenMainTopic is not null)
            {
                associatedScript = _project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == _topic.HiddenMainTopic.EventIndex);
            }
            else
            {
                associatedScript = _project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == _topic.Topic.EventIndex);
            }
            return associatedScript;
        }

        private void UpdateKyonTime()
        {
            _kyonTimeLabel.Text = (_baseTimeStepper.Value * _kyonTimeStepper.Value / 100.0).ToString();
        }
        private void UpdateMikuruTime()
        {
            _mikuruTimeLabel.Text = (_baseTimeStepper.Value * _mikuruTimeStepper.Value / 100.0).ToString();
        }
        private void UpdateNagatoTime()
        {
            _nagatoTimeLabel.Text = (_baseTimeStepper.Value * _nagatoTimeStepper.Value / 100.0).ToString();
        }
        private void UpdateKoizumiTime()
        {
            _koizumiTimeLabel.Text = (_baseTimeStepper.Value * _koizumiTimeStepper.Value / 100.0).ToString();
        }
    }
}
