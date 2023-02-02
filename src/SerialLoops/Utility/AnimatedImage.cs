using Eto.Forms;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops.Utility
{
    public class AnimatedImage : Panel
    {
        public List<(SKGuiImage Frame, int Timing)> FramesWithTimings { get; set; }
        public int CurrentFrame { get; set; }
        public UITimer FrameTimer { get; set; }

        public AnimatedImage(IEnumerable<(SKBitmap Frame, int Timing)> framesWithTimings)
        {
            FramesWithTimings = framesWithTimings.Select(f => (new SKGuiImage(f.Frame), f.Timing)).ToList();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            CurrentFrame = 0;
            Padding = 0;
            UpdateImage();
        }

        public void UpdateImage()
        {
            Content = FramesWithTimings[CurrentFrame].Frame;
        }

        public void Play()
        {
            FrameTimer = new();
            FrameTimer.Elapsed += FrameTimer_Elapsed;
            FrameTimer.Interval = FramesWithTimings[CurrentFrame].Timing / 60.0;
            FrameTimer.Start();
        }

        private void FrameTimer_Elapsed(object sender, EventArgs e)
        {
            FrameTimer.Stop();
            CurrentFrame++;
            if (CurrentFrame >= FramesWithTimings.Count)
            {
                CurrentFrame = 0;
            }
            UpdateImage();
            Play();
        }
    }
}
