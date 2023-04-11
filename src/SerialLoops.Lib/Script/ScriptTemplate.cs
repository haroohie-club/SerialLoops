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

        public void Apply(ScriptItem script)
        {
            foreach (TemplateSection section in Sections)
            {
                int sectionIndex = script.Event.ScriptSections.FindIndex(s => s.Name == section.Name);
                if (sectionIndex >= 0)
                {
                    if (section.CommandsPosition == CommandsPosition.TOP)
                    {
                        new ScriptItemCommand() {  }
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
