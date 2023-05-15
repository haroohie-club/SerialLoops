using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script
{

    public class ScriptTemplate
    {
        public TemplateSection[] Sections { get; set; }

        public ScriptTemplate(params TemplateSection[] sections)
        {
            Sections = sections;
        }

        public void Apply(ScriptItem script, Project project)
        {
            foreach (TemplateSection section in Sections)
            {
                int sectionIndex = script.Event.ScriptSections.FindIndex(s => s.Name == section.Name);
                if (sectionIndex < 0)
                {
                    sectionIndex = script.Event.ScriptSections.Count;
                    script.Event.ScriptSections.Add(new() { Name = section.Name, CommandsAvailable = EventFile.CommandsAvailable });
                }

                if (section.CommandsPosition == CommandsPosition.TOP)
                {
                    for (int i = 0; i < section.Commands.Length; i++)
                    {
                        script.Event.ScriptSections[sectionIndex].Objects.Insert(i,
                            new ScriptItemCommand(script.Event.ScriptSections[sectionIndex], script.Event, i, project, section.Commands[i].Verb, section.Commands[i].Parameters.ToArray()).Invocation);
                    }
                }
                else
                {
                    for (int i = section.Commands.Length - 1; i >= 0; i--)
                    {
                        script.Event.ScriptSections[sectionIndex].Objects.Insert(script.Event.ScriptSections[sectionIndex].Objects.Count - 1,
                            new ScriptItemCommand(script.Event.ScriptSections[sectionIndex], script.Event, i, project, section.Commands[i].Verb, section.Commands[i].Parameters.ToArray()).Invocation);
                    }
                }
            }
        }
    }

    public enum CommandsPosition
    {
        TOP,
        BOTTOM
    }

    public struct TemplateSection
    {
        public string Name { get; set; }
        public CommandsPosition CommandsPosition { get; set; }
        public ScriptItemCommand[] Commands { get; set; }

        public TemplateSection(string name, CommandsPosition commandsPosition, params ScriptItemCommand[] commands)
        {
            Name = name;
            CommandsPosition = commandsPosition;
            Commands = commands;
        }
    }
}
