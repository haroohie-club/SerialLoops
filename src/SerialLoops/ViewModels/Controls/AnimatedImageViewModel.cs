using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Models;
using SkiaSharp;

namespace SerialLoops.ViewModels.Controls;

public class AnimatedImageViewModel : ViewModelBase
{
    [Reactive]
    public SKBitmap CurrentFrame { get; set; }
    public int CurrentFrameIndex { get; set; }

    public ObservableCollection<ReactiveFrameWithTiming> FramesWithTimings { get; }
    public DispatcherTimer FrameTimer { get; set; }

    public AnimatedImageViewModel(IEnumerable<(SKBitmap Frame, int Timing)> framesWithTimings, Action changeTimingCallback = null)
    {
        FramesWithTimings = [.. framesWithTimings.Select(f => new ReactiveFrameWithTiming(f.Frame, f.Timing, changeTimingCallback))];
        Initialize();
    }
    public AnimatedImageViewModel(IEnumerable<(SKBitmap Frame, short Timing)> framesWithTimings, Action changeTimingCallback = null)
    {
        FramesWithTimings = [.. framesWithTimings.Select(f => new ReactiveFrameWithTiming(f.Frame, f.Timing, changeTimingCallback))];
        Initialize();
    }

    private void Initialize()
    {
        CurrentFrameIndex = 0;
        UpdateImage();
        Play();
    }

    private void UpdateImage()
    {
        if (FramesWithTimings.Count > 0)
        {
            CurrentFrame = FramesWithTimings[CurrentFrameIndex].Frame;
        }
        else
        {
            CurrentFrame = null;
        }
    }

    public void Play()
    {
        FrameTimer = new();
        FrameTimer.Tick += FrameTimer_Elapsed;
        FrameTimer.Interval = TimeSpan.FromSeconds(FramesWithTimings[CurrentFrameIndex].Timing / 60.0);
        FrameTimer.Start();
    }

    public void Stop()
    {
        FrameTimer.Stop();
    }

    private void FrameTimer_Elapsed(object sender, EventArgs args)
    {
        FrameTimer.Stop();
        CurrentFrameIndex++;
        if (CurrentFrameIndex >= FramesWithTimings.Count)
        {
            CurrentFrameIndex = 0;
        }
        UpdateImage();
        Play();
    }
}
