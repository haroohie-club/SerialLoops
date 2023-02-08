using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class CharacterSpriteItem : Item, IPreviewableGraphic
    {
        public HaruhiChokuretsuLib.Archive.Data.CharacterSprite Sprite { get; set; }
        public int Index { get; set; }
        public (string ScriptName, ScriptCommandInvocation command)[] ScriptUses { get; set; }

        public CharacterSpriteItem(HaruhiChokuretsuLib.Archive.Data.CharacterSprite sprite, CharacterDataFile chrdata, Project project) : base($"SPR_{sprite.Character}_{chrdata.Sprites.IndexOf(sprite):D3}", ItemType.Character_Sprite)
        {
            Sprite = sprite;
            Index = chrdata.Sprites.IndexOf(sprite);
            PopulateScriptUses(project.Evt);
        }

        public override void Refresh(Project project)
        {
            PopulateScriptUses(project.Evt);
        }

        public List<(SKBitmap frame, int timing)> GetClosedMouthAnimation(Project project)
        {
            MessageInfoFile messageInfo = project.Dat.Files.First(f => f.Name == "MESSINFOS").CastTo<MessageInfoFile>();
            return Sprite.GetClosedMouthAnimation(project.Grp, messageInfo);
        }

        public List<(SKBitmap frame, int timing)> GetLipFlapAnimation(Project project)
        {
            MessageInfoFile messageInfo = project.Dat.Files.First(f => f.Name == "MESSINFOS").CastTo<MessageInfoFile>();
            return Sprite.GetLipFlapAnimation(project.Grp, messageInfo);
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
