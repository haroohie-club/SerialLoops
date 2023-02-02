using HaruhiChokuretsuLib.Archive.Data;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class CharacterSpriteItem : Item
    {
        public CharacterSprite Sprite { get; set; }

        public CharacterSpriteItem(CharacterSprite sprite) : base($"SPR_{sprite.Character}_{sprite.MouthAnimationIndex}_{sprite.EyeAnimationIndex}", ItemType.Character_Sprite)
        {
            Sprite = sprite;
        }

        public override void Refresh(Project project)
        {
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
    }
}
