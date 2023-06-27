using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System.Linq;

namespace SerialLoops.Editors
{
    public class TutorialEditor : Editor
    {
        private TutorialItem _tutorial;

        public TutorialEditor(TutorialItem tutorial, Project project, EditorTabsPanel tabs, ILogger log) : base(tutorial, log, project, tabs)
        {
        }

        public override Container GetEditorPanel()
        {
            _tutorial = (TutorialItem)Description;

            Label idLabel = new() { Text = _tutorial.Tutorial.Id.ToString() };
            DropDown associatedScriptDropDown = new();
            associatedScriptDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Script).Select(s => new ListItem { Key = s.DisplayName, Text = s.DisplayName }));
            ScriptItem associatedScript = (ScriptItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == _tutorial.Tutorial.AssociatedScript);
            associatedScriptDropDown.SelectedKey = associatedScript.DisplayName;
            StackLayout associatedLink = ControlGenerator.GetFileLink(associatedScript, _tabs, _log);

            StackLayout scriptLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("Associated Script", associatedScriptDropDown),
                    associatedLink,
                }
            };

            associatedScriptDropDown.SelectedKeyChanged += (sender, args) =>
            {
                ScriptItem newAssociatedScript = (ScriptItem)_project.Items.First(i => i.DisplayName == associatedScriptDropDown.SelectedKey);
                _tutorial.Tutorial.AssociatedScript = (short)newAssociatedScript.Event.Index;
                scriptLayout.Items.RemoveAt(1);
                scriptLayout.Items.Add(ControlGenerator.GetFileLink(newAssociatedScript, _tabs, _log));

                UpdateTabTitle(false);
            };

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    ControlGenerator.GetControlWithLabel("ID", idLabel),
                    scriptLayout,
                }
            };
        }
    }
}
