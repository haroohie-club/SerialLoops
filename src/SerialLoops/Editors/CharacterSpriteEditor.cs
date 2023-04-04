using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;

namespace SerialLoops.Editors
{
    public class CharacterSpriteEditor : Editor
    {
        private CharacterSpriteItem _sprite;
        private AnimatedImage _animatedImage;

        public CharacterSpriteEditor(CharacterSpriteItem item, EditorTabsPanel tabs, Project project, ILogger log) : base(item, tabs, log, project)
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
