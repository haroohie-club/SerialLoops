using HaruhiChokuretsuLib.Archive.Data;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class CharacterSpriteItem : Item
    {
        public CharacterSprite Sprite { get; set; }
        public List<(SKBitmap Frame, int Timing)> EyeAnimation { get; set; }
        public List<(SKBitmap Frame, int Timing)> LipFlapAnimation { get; set; }

        public CharacterSpriteItem(CharacterSprite sprite, Project project) : base($"SPR_{sprite.Character}_{sprite.MouthAnimationIndex}_{sprite.EyeAnimationIndex}", ItemType.Character_Sprite)
        {
            MessageInfoFile messageInfo = project.Dat.Files.First(f => f.Name == "MESSINFOS").CastTo<MessageInfoFile>();
            Sprite = sprite;
            EyeAnimation = Sprite.GetClosedMouthAnimation(project.Grp, messageInfo);
            LipFlapAnimation = Sprite.GetLipFlapAnimation(project.Grp, messageInfo);
        }

        public override void Refresh(Project project)
        {
            MessageInfoFile messageInfo = project.Dat.Files.First(f => f.Name == "MESSINFOS").CastTo<MessageInfoFile>();
            EyeAnimation = Sprite.GetClosedMouthAnimation(project.Grp, messageInfo);
            LipFlapAnimation = Sprite.GetLipFlapAnimation(project.Grp, messageInfo);
        }
    }
}
