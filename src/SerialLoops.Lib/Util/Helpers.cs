using HaruhiChokuretsuLib.Archive.Event;
using System.Collections.Generic;
using System.Linq;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Lib.Util
{
    public static class Extensions
    {
        public static string GetSubstitutedString(this string line, Project project)
        {
            return string.Join("", line.Select(c => project.FontReplacement.ReverseLookup(c)?.ReplacedCharacter ?? c));
        }

        public static void CollectGarbage(this EventFile evt)
        {
            IEnumerable<string> conditionalContainingCommands = new CommandVerb[] { CommandVerb.VGOTO, CommandVerb.SCENE_GOTO, CommandVerb.SCENE_GOTO2 }.Select(c => c.ToString());
            List<UsedIndex> usedIndices = new();
            foreach (ScriptCommandInvocation conditionalCommand in evt.ScriptSections.SelectMany(s => s.Objects).Where(c => conditionalContainingCommands.Contains(c.Command.Mnemonic)))
            {
                usedIndices.Add(new() { Command = conditionalCommand, Index = conditionalCommand.Parameters[0] });
            }
            if (usedIndices.DistinctBy(c => c.Index).Count() < evt.ConditionalsSection.Objects.Count)
            {
                for (short i = 0; i < evt.ConditionalsSection.Objects.Count; i++)
                {
                    if (!usedIndices.Select(idx => idx.Index).Contains(i))
                    {
                        evt.ConditionalsSection.Objects.RemoveAt(i);
                        for (int j = 0; j < usedIndices.Count; j++)
                        {
                            if (usedIndices[j].Index >= i)
                            {
                                usedIndices[j].Command.Parameters[0]--;
                                usedIndices[j].Index--;
                            }
                        }
                        i--;
                    }
                }
            }
        }

        private class UsedIndex
        {
            public ScriptCommandInvocation Command { get; set; }
            public short Index { get; set; }
        }
    }
}
