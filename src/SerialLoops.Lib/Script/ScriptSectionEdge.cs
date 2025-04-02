using HaruhiChokuretsuLib.Archive.Event;
using QuikGraph;

namespace SerialLoops.Lib.Script;

public class ScriptSectionEdge : IEdge<ScriptSection>
{
    public ScriptSection Source { get; set; }
    public int SourceCommandIndex { get; set; }

    public ScriptSection Target { get; set; }
}
