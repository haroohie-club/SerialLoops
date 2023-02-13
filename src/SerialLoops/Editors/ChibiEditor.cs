using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace SerialLoops.Editors
{
    public class ChibiEditor : Editor
    {
        private ChibiItem _chibi;
        private DropDown _animationSelection;
        private RadioButtonList _poseSelection;

        private AnimatedImage _animatedImage;
        private StackLayout _framesStack;

        public ChibiEditor(ChibiItem chibi, ILogger log, IProgressTracker tracker) : base(chibi, log, tracker)
        {
        }

        public override Container GetEditorPanel()
        {
            _chibi = (ChibiItem)Description;

            TableRow row = new(GetAnimationEditor(), GetChibiSelector());
            row.ScaleHeight = false;

            return new TableLayout(row);
        }

        private Container GetChibiSelector()
        {
            _animationSelection = new();
            _animationSelection.Items.AddRange(_chibi.ChibiEntries
                .Select(c => c.Name.Substring(0, c.Name.Length - 3)).Distinct().Select(c => new ListItem() { Text = c, Key = c }));
            _animationSelection.SelectedIndex = 0;
            _animationSelection.SelectedKeyChanged += ChibiSelection_SelectedKeyChanged;

            _poseSelection = new RadioButtonList();
            _poseSelection.Items.AddRange(Enum.GetValues(typeof(ChibiPose))
                .Cast<ChibiPose>().Select(p => new ListItem() { Text = p.ToString(), Key = p.ToString() }));
            _poseSelection.SelectedIndex = 0;
            _poseSelection.SelectedKeyChanged += ChibiSelection_SelectedKeyChanged;
            _poseSelection.Orientation = Orientation.Vertical;

            return new TableLayout
            {
                Width = 200,
                Padding = 10,
                Rows =
                {
                    new GroupBox
                    {
                        Text = "Animation",
                        Padding = 10,
                        Content = _animationSelection
                    },
                    new GroupBox
                    {
                        Text = "Pose",
                        Padding = 10,
                        Content = _poseSelection
                    }
                }
            };
        }

        private Container GetAnimationEditor()
        {
            _animatedImage = new(_chibi.ChibiAnimations.FirstOrDefault().Value);
            _animatedImage.Play();

            _framesStack = new() { 
                Orientation = Orientation.Horizontal, 
                Spacing = 15,
                MinimumSize = new(300, 100)
            };
            UpdateFramesStack();

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
                Items =
                {
                    new GroupBox
                    {
                        Text = _chibi.Name,
                        Padding = 10,
                        Content = _animatedImage
                    },
                    new GroupBox
                    {
                        Text = "Frames",
                        Padding = 10,
                        Content = new Scrollable
                        {
                            Content = _framesStack
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
            string selectedAnimationKey = getSelectedAnimationKey();
            if (!_chibi.ChibiAnimations.ContainsKey(selectedAnimationKey))
            {
                _animatedImage.FramesWithTimings = new() { (new SKGuiImage(new SkiaSharp.SKBitmap(32, 32)), 1) };
            } else
            {
                AnimatedImage newImage = new(_chibi.ChibiAnimations[selectedAnimationKey]);
                _animatedImage.FramesWithTimings = newImage.FramesWithTimings;
            }
            _animatedImage.CurrentFrame = 0;
            _animatedImage.UpdateImage();
            UpdateFramesStack();
        }

        private string getSelectedAnimationKey()
        {
            return $"{_animationSelection.SelectedKey.Trim()}_{_poseSelection.SelectedKey.Trim()}";
        }

        public enum ChibiPose
        {
            BL,
            BR,
            UL,
            UR
        }
    }
}
