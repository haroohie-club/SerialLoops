using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class ChibiItem : Item, IPreviewableGraphic
    {
        public Chibi Chibi { get; set; }
        public int ChibiIndex { get; set; }
        public List<(string Name, ChibiGraphics Chibi)> ChibiEntries { get; set; } = new();
        public Dictionary<string, bool> ChibiEntryModifications { get; set; } = new();
        public Dictionary<string, List<(SKBitmap Frame, short Timing)>> ChibiAnimations { get; set; } = new();
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
                .Select(c => (project.Grp.Files.First(f => f.Index == c.Animation).Name[0..^3], new ChibiGraphics(c, project))));
            ChibiEntries.ForEach(e => ChibiEntryModifications.Add(e.Name, false));
            ChibiEntries.ForEach(e => ChibiAnimations.Add(e.Name, GetChibiAnimation(e.Name, project.Grp)));
            PopulateScriptUses(project.Evt);
        }

        public void SetChibiAnimation(string entryName, List<(SKBitmap, short)> framesAndTimings)
        {
            ChibiGraphics chibiGraphics = ChibiEntries.First(c => c.Name == entryName).Chibi;
            ChibiEntryModifications[entryName] = true;
            GraphicsFile texture = chibiGraphics.Animation.SetFrameAnimationAndGetTexture(framesAndTimings, chibiGraphics.Texture.Palette);
            texture.Index = chibiGraphics.Texture.Index;
            chibiGraphics.Texture = texture;
        }

        public List<(SKBitmap Frame, short Timing)> GetChibiAnimation(string entryName, ArchiveFile<GraphicsFile> grp)
        {
            ChibiGraphics chibiGraphics = ChibiEntries.First(c => c.Name == entryName).Chibi;
            GraphicsFile animation = chibiGraphics.Animation;

            IEnumerable<SKBitmap> frames = animation.GetAnimationFrames(chibiGraphics.Texture).Select(f => f.GetImage());
            IEnumerable<short> timings = animation.AnimationEntries.Select(a => ((FrameAnimationEntry)a).Time);

            return frames.Zip(timings).ToList();
        }

        public override void Refresh(Project project, ILogger log)
        {
            ChibiAnimations.Clear();
            ChibiEntries.ForEach(e => ChibiAnimations.Add(e.Name, GetChibiAnimation(e.Name, project.Grp)));
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
            DOWN_LEFT = 0,
            DOWN_RIGHT = 1,
            UP_LEFT = 2,
            UP_RIGHT = 3
        }

        public static Direction CodeToDirection(string code)
        {
            return code switch
            {
                "BR" => Direction.DOWN_RIGHT,
                "UL" => Direction.UP_LEFT,
                "UR" => Direction.UP_RIGHT,
                _ => Direction.DOWN_LEFT,
            };
        }

        public class ChibiGraphics
        {
            public GraphicsFile Texture { get; set; }
            public GraphicsFile Animation { get; set; }

            public ChibiGraphics(ChibiEntry entry, Project project)
            {
                Texture = project.Grp.Files.First(g => g.Index == entry.Texture);
                Animation = project.Grp.Files.First(g => g.Index == entry.Animation);
            }

            public void Write(Project project, ILogger log)
            {
                using MemoryStream textureStream = new();
                Texture.GetImage().Encode(textureStream, SKEncodedImageFormat.Png, 1);
                IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{Texture.Index:X3}.png"), textureStream.ToArray(), project, log);
                IO.WriteStringFile(Path.Combine("assets", "graphics", $"{Texture.Index:X3}_pal.csv"), string.Join(',', Texture.Palette.Select(c => c.ToString())), project, log);

                IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{Animation.Index:X3}.bna"), Animation.GetBytes(), project, log);
            }
        }
    }
}
