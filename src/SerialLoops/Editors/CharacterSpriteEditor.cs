using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class CharacterSpriteEditor : Editor
    {
        private CharacterSpriteItem _sprite;
        private AnimatedImage _animatedImage;

        public CharacterSpriteEditor(CharacterSpriteItem item, Project project, ILogger log) : base(item, log, project)
        {
        }

        public override Panel GetEditorPanel()
        {
            _sprite = (CharacterSpriteItem)Description;
            _animatedImage = new AnimatedImage(_sprite.GetLipFlapAnimation(_project));
            _animatedImage.Play();

            return new StackLayout
            {
                Items =
                {
                    _animatedImage
                },
            };
        }
    }
}
