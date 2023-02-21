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
    public class GroupSelectionEditor : Editor
    {
        private GroupSelectionItem _selection;

        public GroupSelectionEditor(GroupSelectionItem selection, ILogger log, Project project, EditorTabsPanel tabs) : base(selection, log, project, tabs)
        {
        }

        public override Container GetEditorPanel()
        {
            _selection = (GroupSelectionItem)Description;

            List<GroupBox> groups = new();
            foreach (ScenarioRouteSelectionStruct routeSelection in _selection.Selection.RouteSelections)
            {
                if (routeSelection is null)
                {
                    continue;
                }

                GroupBox selectionBox = new() { Text = routeSelection.Title.GetSubstitutedString(_project) };

                GroupBox optimalGroupBox = new() { Text = "Optimal Group" };
                StackLayout optimalGroupLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Items =
                    {
                        routeSelection.OptimalGroup.Item1.ToString(),
                        routeSelection.OptimalGroup.Item2.ToString(),
                        routeSelection.OptimalGroup.Item3.ToString(),
                    }
                };
                optimalGroupBox.Content = optimalGroupLayout;

                GroupBox worstGroupBox = new() { Text = "Worst Group" };
                StackLayout worstGroupLayout = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Items =
                    {
                        routeSelection.WorstGroup.Item1.ToString(),
                        routeSelection.WorstGroup.Item2.ToString(),
                        routeSelection.WorstGroup.Item3.ToString(),
                    }
                };
                worstGroupBox.Content = worstGroupLayout;

                GroupBox routesBox = new() { Text = "Routes" };
                StackLayout routesLayout = new() { Orientation = Orientation.Vertical, Spacing = 2 };
                foreach (ScenarioRouteStruct route in routeSelection.Routes)
                {
                    GroupBox routeBox = new() { Text = route.Title.GetSubstitutedString(_project) };
                    StackLayout routeLayout = new()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 2,
                        Items =
                        {
                            ControlGenerator.GetControlWithLabel("Script", ControlGenerator.GetFileLink(_project.Items.First(i => i.Type == ItemDescription.ItemType.Script && ((ScriptItem)i).Event.Index == route.ScriptIndex), _tabs, _log)),
                            ControlGenerator.GetControlWithLabel("Characters Involved", new Label { Text = string.Join(", ", route.CharactersInvolved) }),
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
                        ControlGenerator.GetControlWithLabel("Haruhi Present", new CheckBox { Checked = routeSelection.HaruhiPresent }),
                        ControlGenerator.GetControlWithLabel("Required Brigade Member", new Label { Text = routeSelection.RequiredBrigadeMember.ToString() }),
                        ControlGenerator.GetControlWithLabel("Future Description", new TextBox { Text = routeSelection.FutureDesc.GetSubstitutedString(_project), Width = 400 }),
                        ControlGenerator.GetControlWithLabel("Past Description", new TextBox { Text = routeSelection.PastDesc.GetSubstitutedString(_project), Width = 400 }),
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
