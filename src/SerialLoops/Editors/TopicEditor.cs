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

            StackLayout shortsLayout = new();
            for (int i = 0; i < _topic.Topic.UnknownShorts.Length; i++)
            {
                shortsLayout.Items.Add(ControlGenerator.GetControlWithLabel($"Short {i}", new TextBox() { Text = _topic.Topic.UnknownShorts[i].ToString() }));
            }

            return new TableLayout(ControlGenerator.GetControlWithLabelTable("ID", idLabel),
                ControlGenerator.GetControlWithLabel("Title", titleTextBox),
                ControlGenerator.GetControlWithLabel("Associated Script", linkedScriptDropDown),
                shortsLayout);
        }
    }
}
