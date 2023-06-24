using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Editors;
using SerialLoops.Lib.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Controls
{
    public class ScriptCommandListPanel : Panel
    {
        public Dictionary<ScriptSection, List<ScriptItemCommand>> Commands
        {
            protected get { return _commands; }
            set
            {
                _commands = value;
                try
                {
                    Viewer?.SetContents(GetSections(), _expandItems);
                }
                catch (Exception ex)
                {
                    _log.LogException("Failed to set script command list panel section content.", ex);
                }
            }
        }
        public ScriptCommandSectionTreeGridView Viewer { get; private set; }

        protected ILogger _log;
        private readonly Size _size;
        private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands;
        private readonly bool _expandItems;
        private readonly Editor _editor;

        public ScriptCommandListPanel(Dictionary<ScriptSection, List<ScriptItemCommand>> commands, Size size, bool expandItems, Editor editor, ILogger log)
        {
            Commands = commands;
            _log = log;
            _size = size;
            _expandItems = expandItems;
            _editor = editor;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            try
            {
                Viewer = new ScriptCommandSectionTreeGridView(GetSections(), _editor, _size, _expandItems, _log);
            }
            catch (Exception ex)
            {
                _log.LogException("Failed to set viewr contents for script command list panel.", ex);
            }
            MinimumSize = _size;
            Padding = 0;
            Content = new TableLayout(Viewer.Control);

            Viewer.RepositionCommand += (o, e) =>
            {
                var treeGridView = (ScriptCommandSectionTreeGridView)o;
                var entry = (ScriptCommandSectionEntry)treeGridView.SelectedItem;
                var args = (ScriptCommandSectionEventArgs)e;

                if (args.NewParent.Text == "Top")
                {
                    ScriptSection section = entry.ScriptFile.ScriptSections.First(s => s.Name.Replace("/", "") == entry.Text);
                    entry.ScriptFile.ScriptSections.Remove(section);
                    entry.ScriptFile.ScriptSections.Insert(args.NewIndex, section);

                    LabelsSectionEntry label = entry.ScriptFile.LabelsSection.Objects.FirstOrDefault(l => l.Name.Replace("/", "") == entry.Text);
                    if (label is not null)
                    {
                        entry.ScriptFile.LabelsSection.Objects.Remove(label);
                        entry.ScriptFile.LabelsSection.Objects.Insert(args.NewIndex, label);
                    }
                }
                else
                {
                    ScriptCommandInvocation command = entry.Command.Script.ScriptSections[entry.Command.Script.ScriptSections.IndexOf(entry.Command.Section)]
                        .Objects[entry.Command.Index];
                    entry.Command.Script.ScriptSections[entry.Command.Script.ScriptSections.IndexOf(entry.Command.Section)]
                        .Objects.RemoveAt(entry.Command.Index);
                    _commands[entry.Command.Section].RemoveAt(entry.Command.Index);
                    for (int i = entry.Command.Index; i < _commands[entry.Command.Section].Count; i++)
                    {
                        _commands[entry.Command.Section][i].Index--;
                    }
                    entry.Command.Section = entry.Command.Script.ScriptSections.First(s => s.Name.Replace("/", "").Equals(args.NewParent.Text));
                    entry.Command.Section.Objects.Insert(args.NewIndex, command);
                    entry.Command.Index = args.NewIndex;
                    _commands[entry.Command.Section].Insert(args.NewIndex, entry.Command);
                    for (int i = args.NewIndex + 1; i < _commands[entry.Command.Section].Count; i++)
                    {
                        _commands[entry.Command.Section][i].Index++;
                    }
                }
                ((ScriptEditor)_editor).PopulateScriptCommands(true);
                _editor.UpdateTabTitle(false);
            };
            Viewer.DeleteCommand += (o, e) =>
            {
                var treeGridView = (ScriptCommandSectionTreeGridView)o;
                
                if (e.Item.Text.StartsWith("NONE") || e.Item.Text.StartsWith("SCRIPT"))
                {
                    e.Item.Script.ScriptSections.Remove(e.Item.Script.ScriptSections.First(s => s.Name.Replace("/", "") == e.Item.Text));
                    LabelsSectionEntry label = e.Item.Script.LabelsSection.Objects.FirstOrDefault(l => l.Name.Replace("/", "") == e.Item.Text);
                    if (label is not null)
                    {
                        e.Item.Script.LabelsSection.Objects.Remove(label);
                    }
                    e.Item.Script.NumSections--;
                }
                else
                {
                    ScriptCommandInvocation command = e.Item.Command.Script.ScriptSections[e.Item.Command.Script.ScriptSections.IndexOf(e.Item.Command.Section)]
                        .Objects[e.Item.Command.Index];
                    for (int i = e.Item.Command.Section.Objects.IndexOf(command); i < e.Item.Command.Section.Objects.Count; i++)
                    {
                        _commands[e.Item.Command.Section][i].Index--;
                    }
                    e.Item.Command.Section.Objects.Remove(command);
                    _commands[e.Item.Command.Section].Remove(e.Item.Command);
                }
                ((ScriptEditor)_editor).PopulateScriptCommands(true);
                _editor.UpdateTabTitle(false);
            };
            Viewer.AddCommand += (o, e) =>
            {
                var treeGridView = (ScriptCommandSectionTreeGridView)o;
                var entry = (ScriptCommandSectionEntry)treeGridView.SelectedItem;

                if (string.IsNullOrEmpty(e.SectionTitle))
                {
                    ScriptSection section = entry.Command?.Section ?? e.Command.Section;
                    e.Command.Script.ScriptSections[e.Command.Script.ScriptSections.IndexOf(section)].Objects.Insert(e.Command.Index, e.Command.Invocation);
                    _commands[section].Insert(e.Command.Index, e.Command);
                    for (int i = section.Objects.IndexOf(e.Command.Invocation) + 1; i < section.Objects.Count; i++)
                    {
                        _commands[section][i].Index++;
                    }
                }
                else
                {
                    string sectionName = e.SectionTitle;
                    EventFile scriptFile = entry.ScriptFile is null ? entry.Command.Script : entry.ScriptFile;
                    scriptFile.ScriptSections.Add(new()
                    {
                        Name = sectionName,
                        CommandsAvailable = EventFile.CommandsAvailable,
                        Objects = new(),
                        SectionType = typeof(ScriptSection),
                        ObjectType = typeof(ScriptCommandInvocation),
                    });
                    scriptFile.NumSections++;
                    scriptFile.LabelsSection.Objects.Insert(scriptFile.LabelsSection.Objects.Count - 1, new()
                    {
                        Name = $"NONE/{sectionName[4..]}",
                        Id = (short)(scriptFile.LabelsSection.Objects.Max(l => l.Id) + 1)
                    });
                    _commands.Add(scriptFile.ScriptSections.Last(), new());
                }

                ((ScriptEditor)_editor).PopulateScriptCommands(true);
                _editor.UpdateTabTitle(false);
            };
        }

        private IEnumerable<ScriptCommandSectionEntry> GetSections()
        {
            return Commands.Select(s => new ScriptCommandSectionEntry(s.Key, s.Value.Select(c => new ScriptCommandSectionEntry(c)), Commands.Values.First().FirstOrDefault().Script));
        }
    }
}
