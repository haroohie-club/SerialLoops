using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class CharacterSpriteItem : Item, IPreviewableGraphic
    {
        public CharacterSprite Sprite { get; set; }
        public int Index { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public CharacterSpriteItem(CharacterSprite sprite, CharacterDataFile chrdata, Project project) : base($"SPR_{sprite.Character}_{chrdata.Sprites.IndexOf(sprite):D3}", ItemType.Character_Sprite)
        {
            Sprite = sprite;
            Index = chrdata.Sprites.IndexOf(sprite);
            PopulateScriptUses(project.Evt);
        }

        public override void Refresh(Project project, ILogger log)
        {
            PopulateScriptUses(project.Evt);
        }

        public List<(SKBitmap Frame, int Timing)> GetClosedMouthAnimation(Project project)
        {
            return Sprite.GetClosedMouthAnimation(project.Grp, project.MessInfo);
        }

        public List<(SKBitmap Frame, int Timing)> GetLipFlapAnimation(Project project)
        {
            return Sprite.GetLipFlapAnimation(project.Grp, project.MessInfo);
        }

        public SKBitmap GetBaseLayout(Project project)
        {
            List<GraphicsFile> textures = new() { project.Grp.Files.First(f => f.Index == Sprite.TextureIndex1), project.Grp.Files.First(f => f.Index == Sprite.TextureIndex2), project.Grp.Files.First(f => f.Index == Sprite.TextureIndex3) };
            GraphicsFile layout = project.Grp.Files.First(g => g.Index == Sprite.LayoutIndex);
            (SKBitmap spriteBitmap, _) = layout.GetLayout(textures, 0, layout.LayoutEntries.Count, darkMode: false, preprocessedList: true);

            return spriteBitmap;
        }

        public List<(SKBitmap Frame, short Timing)> GetEyeFrames(Project project)
        {
            GraphicsFile eyeTexture = project.Grp.Files.First(f => f.Index == Sprite.EyeTextureIndex);
            GraphicsFile eyeAnimation = project.Grp.Files.First(f => f.Index == Sprite.EyeAnimationIndex);

            return eyeAnimation.GetAnimationFrames(eyeTexture).Select(g => g.GetImage()).Zip(eyeAnimation.AnimationEntries.Select(a => ((FrameAnimationEntry)a).Time)).ToList();
        }

        public List<(SKBitmap Frame, short Timing)> GetMouthFrames(Project project)
        {
            GraphicsFile mouthTexture = project.Grp.Files.First(f => f.Index == Sprite.MouthTextureIndex);
            GraphicsFile mouthAnimation = project.Grp.Files.First(f => f.Index == Sprite.MouthAnimationIndex);

            return mouthAnimation.GetAnimationFrames(mouthTexture).Select(g => g.GetImage()).ToList().Zip(mouthAnimation.AnimationEntries.Select(a => ((FrameAnimationEntry)a).Time)).ToList();
        }

        public void PopulateScriptUses(ArchiveFile<EventFile> evt)
        {
            ScriptUses = evt.Files.SelectMany(e =>
                e.ScriptSections.SelectMany(sec =>
                    sec.Objects.Where(c => c.Command.Mnemonic == "DIALOGUE").Select(c => (e.Name[0..^1], c))))
                .Where(t => t.c.Parameters[1] == Index).ToArray();
        }
        
        public SKBitmap GetPreview(Project project)
        {
            return GetClosedMouthAnimation(project).First().Frame;
        }
    }
}
