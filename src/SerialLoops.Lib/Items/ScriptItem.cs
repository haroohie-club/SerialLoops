using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using QuikGraph;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using System;
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
        public ScriptItem(EventFile evt, ILogger log) : base(evt.Name[0..^1], ItemType.Script)
        {
            Event = evt;

            PruneLabelsSection(log);
            Graph.AddVertexRange(Event.ScriptSections);
        }

        public Dictionary<ScriptSection, List<ScriptItemCommand>> GetScriptCommandTree(Project project, ILogger log)
        {
            try
            {
                Dictionary<ScriptSection, List<ScriptItemCommand>> commands = new();
                foreach (ScriptSection section in Event.ScriptSections)
                {
                    commands.Add(section, new());
                    foreach (ScriptCommandInvocation command in section.Objects)
                    {
                        commands[section].Add(ScriptItemCommand.FromInvocation(command, section, commands[section].Count, Event, project, log));
                    }
                }
                return commands;
            }
            catch (Exception ex)
            {
                log.LogException($"Error getting script command tree for script {DisplayName} ({Name})", ex);
                return null;
            }
        }

        public void CalculateGraphEdges(Dictionary<ScriptSection, List<ScriptItemCommand>> commandTree, ILogger log)
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
                            Event.MapCharactersSection?.Objects.Select(c => c.TalkScriptBlock).Contains(l.Id) ?? false)
                            .Select(l => l.Name.Replace("/", "")).Contains(s.Name)).Select(s => new ScriptSectionEdge() { Source = section, Target = s }));
                        Graph.AddEdgeRange(Event.ScriptSections.Where(s =>
                            Event.LabelsSection.Objects.Where(l =>
                            Event.InteractableObjectsSection.Objects.Select(o => o.ScriptBlock).Contains(l.Id))
                            .Select(l => l.Name.Replace("/", "")).Contains(s.Name)).Select(s => new ScriptSectionEdge() { Source = section, Target = s }));
                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.GOTO)
                    {
                        try
                        {
                            Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[0]).Section });
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            log.LogWarning("Failed to add graph edge for GOTO command as script section parameter was out of range.");
                        }
                        @continue = true;
                    }
                    else if (command.Verb == CommandVerb.VGOTO)
                    {
                        try
                        {
                            Graph.AddEdge(new() { Source = section, Target = ((ScriptSectionScriptParameter)command.Parameters[1]).Section });
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            log.LogWarning("Failed to add graph edge for VGOTO command as script section parameter was out of range.");
                        }
                    }
                    else if (command.Verb == CommandVerb.CHESS_VGOTO)
                    {
                        Graph.AddEdgeRange(command.Parameters.Cast<ScriptSectionScriptParameter>()
                            .Where(p => p.Section is not null).Select(p => new ScriptSectionEdge() { Source = section, Target = p.Section }));
                        ScriptSection miss2Section = Event.ScriptSections.FirstOrDefault(s => s.Name == "NONEMiss2");
                        if (miss2Section is not null)
                        {
                            Graph.AddEdge(new() { Source = section, Target = Event.ScriptSections.First(s => s.Name == "NONEMiss2") }); // hardcode this section, even tho you can't get to it
                        }
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
                    else if (command.Verb == CommandVerb.BACK && section.Name != "SCRIPT00")
                    {
                        @continue = true;
                    }
                    else if (Name.StartsWith("CHS") && Name.EndsWith("90") && commandTree.Keys.ToList().IndexOf(section) > 1 && command.Index == 0)
                    {
                        Graph.AddEdge(new() { Source = Event.ScriptSections[1], Target = section }); // these particular chess files have no VGOTOs, so uh... we manually hardcode them
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

        public void PruneLabelsSection(ILogger log)
        {
            if ((Event.LabelsSection?.Objects?.Count ?? 0) - 1 > Event.ScriptSections.Count)
            {
                try
                {
                    for (int i = 0; i < Event.LabelsSection.Objects.Count; i++)
                    {
                        if (Event.LabelsSection.Objects[i].Id == 0)
                        {
                            continue;
                        }
                        if (!Event.ScriptSections.Select(s => s.Name).Contains(Event.LabelsSection.Objects[i].Name.Replace("/", "")))
                        {
                            Event.LabelsSection.Objects.RemoveAt(i);
                            i--;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogException($"Error pruning labels for script {DisplayName} ({Name})", ex);
                }
            }
        }

        public override void Refresh(Project project, ILogger log)
        {
            Graph = new();
            Graph.AddVertexRange(Event.ScriptSections);
            CalculateGraphEdges(GetScriptCommandTree(project, log), log);
        }
    }
}
