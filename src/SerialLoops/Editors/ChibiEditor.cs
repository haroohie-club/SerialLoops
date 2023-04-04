using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Linq;

namespace SerialLoops.Editors
{
    public class ChibiEditor : Editor
    {
        private ChibiItem _chibi;
        private DropDown _animationSelection;
        private ChibiDirectionSelector _directionSelector;

        private AnimatedImage _animatedImage;
        private StackLayout _framesStack;

        public ChibiEditor(ChibiItem chibi, EditorTabsPanel tabs, ILogger log) : base(chibi, tabs, log)
        {
        }

        public override Container GetEditorPanel()
        {
            _chibi = (ChibiItem)Description;
            return new TableLayout(GetAnimatedChibi(), GetBottomPanel());
        }

        private StackLayout GetAnimatedChibi()
        {
            _animatedImage = new(_chibi.ChibiAnimations.FirstOrDefault().Value);
            _animatedImage.Play();
            return new StackLayout
            {
                Items = { new TableLayout(_animatedImage) },
                Height = Math.Max(250, _animatedImage.Height),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private Container GetBottomPanel()
        {
            _animationSelection = new();
            _animationSelection.Items.AddRange(_chibi.ChibiEntries
                .Select(c => c.Name.Substring(0, c.Name.Length - 3)).Distinct().Select(c => new ListItem() { Text = c, Key = c }));
            _animationSelection.SelectedIndex = 0;
            _animationSelection.SelectedKeyChanged += ChibiSelection_SelectedKeyChanged;

            _directionSelector = new(_log)
            {
                Direction = ChibiItem.Direction.DOWN_RIGHT
            };
            _directionSelector.DirectionChanged += ChibiSelection_SelectedKeyChanged;

            return new TableLayout
            {
                Padding = 10,
                Spacing = new(5, 5),
                Rows =
                {
                    new(
                        new GroupBox
                        {
                            Text = "Animation",
                            Padding = 10,
                            Content = new StackLayout
                            {
                                Spacing = 10,
                                Items =
                                {
                                    _animationSelection, 
                                    _directionSelector
                                }
                            }
                        },
                        new GroupBox
                        {
                            Text = "Frames",
                            Padding = 10,
                            Content = new Scrollable { Content = GetFramesStack() }
                        }
                    )
                }
            };
        }

        private StackLayout GetFramesStack()
        {
            _framesStack = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15
            };
            UpdateFramesStack();
            return _framesStack;
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
            string selectedAnimationKey = GetSelectedAnimationKey();
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

        private string GetSelectedAnimationKey()
        {
            string direction = "";
            switch (_directionSelector.Direction)
            {
                case ChibiItem.Direction.DOWN_LEFT:
                    direction = "BL";
                    break;
                case ChibiItem.Direction.DOWN_RIGHT:
                    direction = "BR";
                    break;
                case ChibiItem.Direction.UP_LEFT:
                    direction = "UL";
                    break;
                case ChibiItem.Direction.UP_RIGHT:
                    direction = "UR";
                    break;
            }
            return $"{_animationSelection.SelectedKey.Trim()}_{direction}";
        }

    }
}
