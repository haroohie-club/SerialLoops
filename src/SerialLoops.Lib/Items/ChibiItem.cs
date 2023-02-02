using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class ChibiItem : Item
    {
        public Chibi Chibi { get; set; }
        public List<(string Name, ChibiEntry Chibi)> ChibiEntries { get; set; } = new();
        public List<IEnumerable<(SKBitmap Frame, int Timing)>> ChibiAnimations { get; set; } = new();

        public ChibiItem(Chibi chibi, Project project) : base($"CHIBI{chibi.ChibiEntries[0].Animation}", ItemType.Chibi)
        {
            Chibi = chibi;
            string firstAnimationName = project.Grp.Files.First(f => f.Index == Chibi.ChibiEntries[0].Animation).Name;
            Name = $"CHIBI_{firstAnimationName[0..firstAnimationName.IndexOf('_')]}";
            ChibiEntries.AddRange(Chibi.ChibiEntries.Where(c => c.Animation > 0)
                .Select(c => (project.Grp.Files.First(f => f.Index == c.Animation).Name[0..^3], c)));
            ChibiAnimations.AddRange(ChibiEntries.Select(c => GetChibiAnimation(c.Name, project.Grp)));
        }

        private IEnumerable<(SKBitmap Frame, int Timing)> GetChibiAnimation(string entryName, ArchiveFile<GraphicsFile> grp)
        {
            ChibiEntry entry = ChibiEntries.First(c => c.Name == entryName).Chibi;
            GraphicsFile animation = grp.Files.First(f => f.Index == entry.Animation);

            IEnumerable<SKBitmap> frames = animation.GetAnimationFrames(grp.Files.First(f => f.Index == entry.Texture)).Select(f => f.GetImage());
            IEnumerable<int> timings = animation.AnimationEntries.Select(a => (int)((FrameAnimationEntry)a).Time);

            return frames.Zip(timings);
        }
    }
}
