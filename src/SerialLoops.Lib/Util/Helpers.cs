using System.Linq;

namespace SerialLoops.Lib.Util
{
    public static class Helpers
    {
        public static string GetSubstitutedString(this string line, Project project)
        {
            return string.Join("", line.Select(c => project.FontReplacement.ReverseLookup(c)?.ReplacedCharacter ?? c));
        }
    }
}
