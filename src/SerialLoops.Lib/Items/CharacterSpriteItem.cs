using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
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

        public List<(SKBitmap frame, int timing)> GetClosedMouthAnimation(Project project)
        {
            return Sprite.GetClosedMouthAnimation(project.Grp, project.MessInfo);
        }

        public List<(SKBitmap frame, int timing)> GetLipFlapAnimation(Project project)
        {
            return Sprite.GetLipFlapAnimation(project.Grp, project.MessInfo);
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
            return GetClosedMouthAnimation(project).First().frame;
        }
    }
}
