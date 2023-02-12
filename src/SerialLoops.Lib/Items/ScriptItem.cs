using HaruhiChokuretsuLib.Archive.Event;
using QuikGraph;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Lib.Items
{
    public class ScriptItem : Item
    {
        public EventFile Event { get; set; }
        public AdjacencyGraph<ScriptSection, ScriptSectionEdge> Graph { get; set; } = new();

        public ScriptItem(string name) : base(name, ItemType.Script)
        {
        }
        public ScriptItem(EventFile evt) : base(evt.Name[0..^1], ItemType.Script)
        {
            Event = evt;

            Graph.AddVertexRange(Event.ScriptSections);
        }

        public void CalculateGraphEdges(Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree)
        {
            foreach (ScriptSection section in commandTree.Keys)
            {
                bool @continue = false;
                foreach (ScriptItemCommand command in commandTree[section])
                {
                    if (command.Verb == CommandVerb.INVEST_START)
                    {
                        Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[4]).Section });
                        Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                            Event.LabelsSection.Objects.Where(l =>
                            Event.MapCharactersSection.Objects.Select(c => c.TalkScriptBlock).Contains(l.Id))
                            .Select(l => l.Name.Replace("/", "")).Contains(s.Name)).Select(s => new ScriptSectionEdge() { Source = section, Target = s }));
                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.GOTO)
                    {
                        Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[0]).Section });
                        continue;
                    }
                    else if (command.Verb == CommandVerb.VGOTO)
                    {
                        Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[1]).Section });
                    }
                    else if (command.Verb == CommandVerb.CHESS_VGOTO)
                    {
                        Graph.AddEdgeRange(command.Parameters.Cast<ScriptSectionScriptParameter>()
                            .Where(p => p.Section is not null).Select(p => new ScriptSectionEdge() { Source = section, Target = p.Section }));
                    }
                    else if (command.Verb == CommandVerb.SELECT)
                    {
                        Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                            Event.LabelsSection.Objects.Where(l =>
                            command.Parameters.Where(p => p.Type == ScriptParameter.ParameterType.OPTION).Cast<OptionScriptParameter>()
                            .Where(p => p.Option.Id > 0).Select(p => p.Option.Id).Contains(l.Id)).Select(l => l.Name.Replace("/", "")).Contains(s.Name))
                            .Select(s => new ScriptSectionEdge() { Source = section, Target = s }));
                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.NEXT_SCENE)
                    {
                        @continue = true;
                    }
                }
                if (@continue)
                {
                    continue;
                }
                if (section != commandTree.Keys.Last())
                {
                    Graph.AddEdge(new() { Source = section, Target = commandTree.Keys.ElementAt(commandTree.Keys.ToList().IndexOf(section) + 1) });
                }
            }
        }

        public override void Refresh(Project project)
        {
        }
    }
}
