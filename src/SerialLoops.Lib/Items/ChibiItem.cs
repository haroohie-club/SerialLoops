using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class ChibiItem : Item, IPreviewableGraphic
    {
        public Chibi Chibi { get; set; }
        public int ChibiIndex { get; set; }
        public List<(string Name, ChibiEntry Chibi)> ChibiEntries { get; set; } = new();
        public Dictionary<string, IEnumerable<(SKBitmap Frame, int Timing)>> ChibiAnimations { get; set; } = new();
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public ChibiItem(Chibi chibi, Project project) : base($"CHIBI{chibi.ChibiEntries[0].Animation}", ItemType.Chibi)
        {
            List<string> chibiIndices = new() { "", "KYN", "HAL", "MIK", "NAG", "KOI" };

            Chibi = chibi;
            string firstAnimationName = project.Grp.Files.First(f => f.Index == Chibi.ChibiEntries[0].Animation).Name;
            Name = $"CHIBI_{firstAnimationName[0..firstAnimationName.IndexOf('_')]}";
            DisplayName = $"CHIBI_{firstAnimationName[0..firstAnimationName.IndexOf('_')]}";
            ChibiIndex = chibiIndices.IndexOf(firstAnimationName[0..3]);
            ChibiEntries.AddRange(Chibi.ChibiEntries.Where(c => c.Animation > 0)
                .Select(c => (project.Grp.Files.First(f => f.Index == c.Animation).Name[0..^3], c)));
            ChibiEntries.ForEach(e => ChibiAnimations.Add(e.Name, GetChibiAnimation(e.Name, project.Grp)));
            PopulateScriptUses(project.Evt);
        }

        private IEnumerable<(SKBitmap Frame, int Timing)> GetChibiAnimation(string entryName, ArchiveFile<GraphicsFile> grp)
        {
            ChibiEntry entry = ChibiEntries.First(c => c.Name == entryName).Chibi;
            GraphicsFile animation = grp.Files.First(f => f.Index == entry.Animation);

            IEnumerable<SKBitmap> frames = animation.GetAnimationFrames(grp.Files.First(f => f.Index == entry.Texture)).Select(f => f.GetImage());
            IEnumerable<int> timings = animation.AnimationEntries.Select(a => (int)((FrameAnimationEntry)a).Time);

            return frames.Zip(timings);
        }

        public override void Refresh(Project project)
        {
            GetChibiAnimation(Name, project.Grp);
            PopulateScriptUses(project.Evt);
        }

        public void PopulateScriptUses(ArchiveFile<EventFile> evt)
        {
            string[] chibiCommands = new string[] { "CHIBI_ENTEREXIT" };

            var list = evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => chibiCommands.Contains(c.Command.Mnemonic)).Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[0] == ChibiIndex).ToList();

            ScriptUses = list.ToArray();
        }

        SKBitmap IPreviewableGraphic.GetPreview(Project project)
        {
            return ChibiAnimations.First().Value.First().Frame;
        }

        public enum Direction
        {
            DOWN_LEFT,
            DOWN_RIGHT,
            UP_LEFT,
            UP_RIGHT
        }
    }
    
}
