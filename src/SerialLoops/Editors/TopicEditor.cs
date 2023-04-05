﻿using Eto.Forms;
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

        private NumericStepper _baseTimeBox;
        private NumericStepper _kyonTimeBox;
        private Label _kyonTimeLabel;
        private NumericStepper _mikuruTimeBox;
        private Label _mikuruTimeLabel;
        private NumericStepper _nagatoTimeBox;
        private Label _nagatoTimeLabel;
        private NumericStepper _koizumiTimeBox;
        private Label _koizumiTimeLabel;

        public TopicEditor(TopicItem topic, Project project, ILogger log) : base(topic, log, project)
        {
        }

        public override Container GetEditorPanel()
        {
            _topic = (TopicItem)Description;

            Label idLabel = new() { Text = _topic.Topic.Id.ToString() };

            TextBox titleTextBox = new() { Text = _topic.Topic.Title.GetSubstitutedString(_project), Width = 200 };

            DropDown linkedScriptDropDown = new();
            linkedScriptDropDown.Items.Add(new ListItem { Key = "NONE", Text = "NONE" });
            linkedScriptDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(s => new ListItem { Key = s.Name, Text = s.Name }));
            linkedScriptDropDown.SelectedKey = _project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == _topic.Topic.EventIndex)?.Name ?? "NONE";

            _baseTimeBox = new() { Value = _topic.Topic.BaseTimeGain, MaxValue = short.MaxValue, MinValue = 0 };
            _baseTimeBox.ValueChanged += (sender, args) =>
            {
                TryUpdateKyonTime();
                TryUpdateMikuruTime();
                TryUpdateNagatoTime();
                TryUpdateKoizumiTime();
            };

            _kyonTimeBox = new() { Value = _topic.Topic.KyonTimePercentage, MaxValue = short.MaxValue, MinValue = 0 };
            _kyonTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.KyonTimePercentage / 100.0)).ToString() };
            _kyonTimeBox.ValueChanged += (sender, args) =>
            {
                TryUpdateKyonTime();
            };

            _mikuruTimeBox = new() { Value = _topic.Topic.MikuruTimePercentage, MaxValue = short.MaxValue, MinValue = 0 };
            _mikuruTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.MikuruTimePercentage / 100.0)).ToString() };
            _mikuruTimeBox.ValueChanged += (sender, args) =>
            {
                TryUpdateMikuruTime();
            };

            _nagatoTimeBox = new() { Value = _topic.Topic.NagatoTimePercentage, MaxValue = short.MaxValue, MinValue = 0 };
            _nagatoTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.NagatoTimePercentage / 100.0)).ToString() };
            _nagatoTimeBox.ValueChanged += (sender, args) =>
            {
                TryUpdateNagatoTime();
            };

            _koizumiTimeBox = new() { Value = _topic.Topic.KoizumiTimePercentage, MaxValue = short.MaxValue, MinValue = 0 };
            _koizumiTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.KoizumiTimePercentage / 100.0)).ToString() };
            _koizumiTimeBox.ValueChanged += (sender, args) =>
            {
                TryUpdateKoizumiTime();
            };

            StackLayout timesLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Base Time Gain", ControlGenerator.GetControlWithSuffix(_baseTimeBox, "sec")),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("Kyon Time Percentage", ControlGenerator.GetControlWithSuffix(_kyonTimeBox, "%")),
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
                            ControlGenerator.GetControlWithLabel("Mikuru Time Percentage", ControlGenerator.GetControlWithSuffix(_mikuruTimeBox, "%")),
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
                            ControlGenerator.GetControlWithLabel("Nagato Time Percentage", ControlGenerator.GetControlWithSuffix(_nagatoTimeBox, "%")),
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
                            ControlGenerator.GetControlWithLabel("Koizumi Time Percentage", ControlGenerator.GetControlWithSuffix(_koizumiTimeBox, "%")),
                            ControlGenerator.GetControlWithSuffix(_koizumiTimeLabel, "sec"),
                        }
                    },
                }
            };

            StackLayout unknownsLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Type", new TextBox { Text = _topic.Topic.Type.ToString() }),
                    ControlGenerator.GetControlWithLabel("Episode Group", new TextBox { Text = _topic.Topic.EpisodeGroup.ToString() }),
                    ControlGenerator.GetControlWithLabel("Group Selection", new TextBox { Text = _topic.Topic.GroupSelection.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 03", new TextBox { Text = _topic.Topic.UnknownShort03.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 04", new TextBox { Text = _topic.Topic.UnknownShort04.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 09", new TextBox { Text = _topic.Topic.UnknownShort09.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 10", new TextBox { Text = _topic.Topic.UnknownShort10.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 11", new TextBox { Text = _topic.Topic.UnknownShort11.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 12", new TextBox { Text = _topic.Topic.UnknownShort12.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 13", new TextBox { Text = _topic.Topic.UnknownShort13.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 14", new TextBox { Text = _topic.Topic.UnknownShort14.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 15", new TextBox { Text = _topic.Topic.UnknownShort15.ToString() }),
                }
            };


            return new TableLayout(ControlGenerator.GetControlWithLabelTable("ID", idLabel),
                ControlGenerator.GetControlWithLabel("Title", titleTextBox),
                ControlGenerator.GetControlWithLabel("Associated Script", linkedScriptDropDown),
                new GroupBox() { Text = "Times", Content = timesLayout },
                unknownsLayout);
        }

        private void TryUpdateKyonTime()
        {
            _kyonTimeLabel.Text = (_baseTimeBox.Value * _kyonTimeBox.Value / 100.0).ToString();
        }
        private void TryUpdateMikuruTime()
        {
            _mikuruTimeLabel.Text = (_baseTimeBox.Value * _mikuruTimeBox.Value / 100.0).ToString();
        }
        private void TryUpdateNagatoTime()
        {
            _nagatoTimeLabel.Text = (_baseTimeBox.Value * _nagatoTimeBox.Value / 100.0).ToString();
        }
        private void TryUpdateKoizumiTime()
        {
            _koizumiTimeLabel.Text = (_baseTimeBox.Value * _koizumiTimeBox.Value / 100.0).ToString();
        }
    }
}
