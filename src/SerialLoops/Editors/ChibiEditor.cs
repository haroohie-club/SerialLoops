using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SerialLoops.Editors
{
    public class ChibiEditor : Editor
    {
        private ChibiItem _chibi;
        private DropDown _chibiSelection;
        private AnimatedImage _animatedImage;

        public ChibiEditor(ChibiItem chibi, ILogger log) : base(chibi, log)
        {
        }

        public override Panel GetEditorPanel()
        {
            _chibi = (ChibiItem)Description;

            _chibiSelection = new();
            _chibiSelection.Items.AddRange(_chibi.ChibiEntries.Select(c => new ListItem() { Text = c.Name, Key = c.Name }));
            _chibiSelection.SelectedIndex = 0;
            _chibiSelection.SelectedKeyChanged += ChibiSelection_SelectedKeyChanged;

            _animatedImage = new(_chibi.ChibiAnimations[0]);
            _animatedImage.Play();

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    _chibiSelection,
                    _animatedImage
                }
            };
        }

        private void ChibiSelection_SelectedKeyChanged(object sender, EventArgs e)
        {
            DropDown chibiDropDown = (DropDown)sender;
            AnimatedImage newImage = new(_chibi.ChibiAnimations[chibiDropDown.SelectedIndex]);
            _animatedImage.CurrentFrame = 0;
            _animatedImage.FramesWithTimings = newImage.FramesWithTimings;
            _animatedImage.UpdateImage();
            _animatedImage.Play();
        }
    }
}
