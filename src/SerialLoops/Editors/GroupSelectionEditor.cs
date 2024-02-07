using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Editors
{
    public class GroupSelectionEditor(GroupSelectionItem selection, ILogger log, Project project, EditorTabsPanel tabs) : Editor(selection, log, project, tabs)
    {
        private GroupSelectionItem _selection;

        public override Container GetEditorPanel()
        {
            _selection = (GroupSelectionItem)Description;

            List<GroupBox> groups = [];
            foreach (ScenarioActivity scenarioActivity in _selection.Selection.Activities)
            {
                if (scenarioActivity is null)
                {
                    continue;
                }

                GroupBox selectionBox = new() { Text = scenarioActivity.Title.GetSubstitutedString(_project) };

                GroupBox optimalGroupBox = new() { Text = Application.Instance.Localize(this, "Optimal Group") };
                StackLayout optimalGroupLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Items =
                    {
                        scenarioActivity.OptimalGroup.Item1.ToString(),
                        scenarioActivity.OptimalGroup.Item2.ToString(),
                        scenarioActivity.OptimalGroup.Item3.ToString(),
                    }
                };
                optimalGroupBox.Content = optimalGroupLayout;

                GroupBox worstGroupBox = new() { Text = Application.Instance.Localize(this, "Worst Group") };
                StackLayout worstGroupLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Items =
                    {
                        scenarioActivity.WorstGroup.Item1.ToString(),
                        scenarioActivity.WorstGroup.Item2.ToString(),
                        scenarioActivity.WorstGroup.Item3.ToString(),
                    }
                };
                worstGroupBox.Content = worstGroupLayout;

                GroupBox routesBox = new() { Text = Application.Instance.Localize(this, "Routes") };
                StackLayout routesLayout = new() { Orientation = Orientation.Vertical, Spacing = 2 };
                foreach (ScenarioRoute route in scenarioActivity.Routes)
                {
                    GroupBox routeBox = new() { Text = $"{route.Title.GetSubstitutedString(_project)} ({route.Flag})" };
                    IEnumerable<TopicItem> kyonlessTopics = route.KyonlessTopics.Select(t => (TopicItem)_project.Items
                        .FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic && ((TopicItem)i).TopicEntry.Id == t));
                    StackLayout kyonlessTopicsLayout = new();
                    foreach (TopicItem topicItem in kyonlessTopics)
                    {
                        if (topicItem is not null)
                        {
                            kyonlessTopicsLayout.Items.Add(ControlGenerator.GetFileLink(topicItem, _tabs, _log));
                        }
                    }
                    GroupBox kyonlessTopicsBox = new() { Text = Application.Instance.Localize(this, "Kyonless Topics"), Content = kyonlessTopicsLayout };
                    StackLayout routeLayout = new()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 2,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Script"), ControlGenerator.GetFileLink(_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == route.ScriptIndex), _tabs, _log)),
                            ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Characters Involved"), new Label { Text = string.Join(", ", route.CharactersInvolved) }),
                            kyonlessTopicsBox,
                        },
                    };
                    routeBox.Content = routeLayout;
                    routesLayout.Items.Add(routeBox);
                }
                routesBox.Content = routesLayout;

                StackLayout selectionLayout = new()
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 3,
                    Items =
                    {
                        ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Haruhi Present"), new CheckBox { Checked = scenarioActivity.HaruhiPresent }),
                        ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Required Brigade Member"), new Label { Text = scenarioActivity.RequiredBrigadeMember.ToString() }),
                        ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Future Description"), new TextBox { Text = scenarioActivity.FutureDesc.GetSubstitutedString(_project), Width = 400 }),
                        ControlGenerator.GetControlWithLabel(Application.Instance.Localize(this, "Past Description"), new TextBox { Text = scenarioActivity.PastDesc.GetSubstitutedString(_project), Width = 400 }),
                        optimalGroupBox,
                        worstGroupBox,
                        routesBox,
                    }
                };

                selectionBox.Content = selectionLayout;
                groups.Add(selectionBox);
            }

            TableLayout tableLayout = new();
            foreach (GroupBox groupBox in groups)
            {
                tableLayout.Rows.Add(groupBox);
            }

            return new Scrollable { Content = tableLayout };
        }
    }
}
