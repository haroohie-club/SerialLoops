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

        private TextBox _baseTimeBox;
        private TextBox _kyonTimeBox;
        private Label _kyonTimeLabel;
        private TextBox _mikuruTimeBox;
        private Label _mikuruTimeLabel;
        private TextBox _nagatoTimeBox;
        private Label _nagatoTimeLabel;
        private TextBox _koizumiTimeBox;
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

            _baseTimeBox = new() { Text = _topic.Topic.BaseTimeGain.ToString() };
            _baseTimeBox.TextChanged += BaseTimeBox_TextChanged;

            _kyonTimeBox = new() { Text = _topic.Topic.KyonTimePercentage.ToString() };
            _kyonTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.KyonTimePercentage / 100.0)).ToString() };
            _kyonTimeBox.TextChanged += KyonTimeBox_TextChanged;

            _mikuruTimeBox = new() { Text = _topic.Topic.MikuruTimePercentage.ToString() };
            _mikuruTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.MikuruTimePercentage / 100.0)).ToString() };
            _mikuruTimeBox.TextChanged += MikuruTimeBox_TextChanged;

            _nagatoTimeBox = new() { Text = _topic.Topic.NagatoTimePercentage.ToString() };
            _nagatoTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.NagatoTimePercentage / 100.0)).ToString() };
            _nagatoTimeBox.TextChanged += NagatoTimeBox_TextChanged;

            _koizumiTimeBox = new() { Text = _topic.Topic.KoizumiTimePercentage.ToString() };
            _koizumiTimeLabel = new() { Text = ((int)(_topic.Topic.BaseTimeGain * _topic.Topic.KoizumiTimePercentage / 100.0)).ToString() };
            _koizumiTimeBox.TextChanged += KoizumiTimeBox_TextChanged; ;

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
                    ControlGenerator.GetControlWithLabel("Unknown 00", new TextBox { Text = _topic.Topic.UnknownShort00.ToString() }),
                    ControlGenerator.GetControlWithLabel("Unknown 01", new TextBox { Text = _topic.Topic.UnknownShort01.ToString() }),
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
                timesLayout,
                unknownsLayout);
        }


        private void BaseTimeBox_TextChanged(object sender, System.EventArgs e)
        {
            if (short.TryParse(_baseTimeBox.Text, out short newBaseTime))
            {
                TryUpdateKyonTime(newBaseTime);
                TryUpdateMikuruTime(newBaseTime);
                TryUpdateNagatoTime(newBaseTime);
                TryUpdateKoizumiTime(newBaseTime);
            }
        }
        private void KyonTimeBox_TextChanged(object sender, System.EventArgs e)
        {
            if (short.TryParse(_baseTimeBox.Text, out short newBaseTime))
            {
                TryUpdateKyonTime(newBaseTime);
            }
        }
        private void MikuruTimeBox_TextChanged(object sender, System.EventArgs e)
        {
            if (short.TryParse(_baseTimeBox.Text, out short newBaseTime))
            {
                TryUpdateMikuruTime(newBaseTime);
            }
        }
        private void NagatoTimeBox_TextChanged(object sender, System.EventArgs e)
        {
            if (short.TryParse(_baseTimeBox.Text, out short newBaseTime))
            {
                TryUpdateNagatoTime(newBaseTime);
            }
        }
        private void KoizumiTimeBox_TextChanged(object sender, System.EventArgs e)
        {
            if (short.TryParse(_baseTimeBox.Text, out short newBaseTime))
            {
                TryUpdateKoizumiTime(newBaseTime);
            }
        }

        private void TryUpdateKyonTime(short baseTime)
        {
            if (short.TryParse(_kyonTimeBox.Text, out short newKyonPercentage))
            {
                _kyonTimeLabel.Text = (baseTime * newKyonPercentage / 100.0).ToString();
            }
        }
        private void TryUpdateMikuruTime(short baseTime)
        {
            if (short.TryParse(_mikuruTimeBox.Text, out short newMikuruPercentage))
            {
                _mikuruTimeLabel.Text = (baseTime * newMikuruPercentage / 100.0).ToString();
            }
        }
        private void TryUpdateNagatoTime(short baseTime)
        {
            if (short.TryParse(_nagatoTimeBox.Text, out short newNagatoPercentage))
            {
                _nagatoTimeLabel.Text = (baseTime * newNagatoPercentage / 100.0).ToString();
            }
        }
        private void TryUpdateKoizumiTime(short baseTime)
        {
            if (short.TryParse(_koizumiTimeBox.Text, out short newKoizumiPercentage))
            {
                _koizumiTimeLabel.Text = (baseTime * newKoizumiPercentage / 100.0).ToString();
            }
        }
    }
}
