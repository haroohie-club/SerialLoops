using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Linq;

namespace SerialLoops.Editors
{
    public class ChibiEditor : Editor
    {
        private ChibiItem _chibi;
        private DropDown _chibiSelection;
        private AnimatedImage _animatedImage;
        private StackLayout _framesStack;

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

            _framesStack = new() { Orientation = Orientation.Vertical, Spacing = 10 };
            UpdateFramesStack();

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Items =
                {
                    _chibiSelection,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 20,
                        Items =
                        {
                            new GroupBox
                            {
                                Text = "Animation",
                                Content = _animatedImage,
                            },
                            new GroupBox
                            {
                                Text = "Frames",
                                Content = _framesStack,
                            }
                        }
                    }
                }
            };
        }

        private void UpdateFramesStack()
        {
            _framesStack.Items.Clear();
            foreach (SKGuiImage image in _animatedImage.FramesWithTimings.Select(f => f.Frame))
            {
                _framesStack.Items.Add(image);
            }
        }

        private void ChibiSelection_SelectedKeyChanged(object sender, EventArgs e)
        {
            DropDown chibiDropDown = (DropDown)sender;
            AnimatedImage newImage = new(_chibi.ChibiAnimations[chibiDropDown.SelectedIndex]);
            _animatedImage.CurrentFrame = 0;
            _animatedImage.FramesWithTimings = newImage.FramesWithTimings;
            _animatedImage.UpdateImage();
            UpdateFramesStack();
        }
    }
}
