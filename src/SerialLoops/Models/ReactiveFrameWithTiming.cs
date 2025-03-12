using System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;

namespace SerialLoops.Models;

public class ReactiveFrameWithTiming(SKBitmap frame, int timing, Action timingChanged = null) : ReactiveObject
{
    public SKBitmap Frame { get; } = frame;
    private int _timing = timing;

    public int Timing
    {
        get => _timing;
        set
        {
            this.RaiseAndSetIfChanged(ref _timing, value);
            timingChanged?.Invoke();
        }
    }
}
