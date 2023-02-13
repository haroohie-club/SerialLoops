using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class CharacterSpriteEditor : Editor
    {
        private CharacterSpriteItem _sprite;
        private AnimatedImage _animatedImage;

        public CharacterSpriteEditor(CharacterSpriteItem item, Project project, ILogger log, IProgressTracker tracker) : base(item, log, tracker, project)
        {
        }

        public override Container GetEditorPanel()
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
