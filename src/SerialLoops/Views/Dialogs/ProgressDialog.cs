using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using SerialLoops.Controls;

namespace SerialLoops.Views.Dialogs;

public class ProgressDialog : Window
{
    private readonly Action _loadingTask;
    private readonly LoopyProgressTracker _tracker;
    private readonly Action _onComplete;

    public ProgressDialog(Action loadingTask, Action onComplete, LoopyProgressTracker tracker, string title)
    {
        _loadingTask = loadingTask;
        _tracker = tracker;
        _onComplete = onComplete;

        Icon = new(AssetLoader.Open(new("avares://SerialLoops/Assets/serial-loops.ico")));
        _tracker.TitleText.Text = title;
        Title = title;

        MinWidth = 350;
        MinHeight = 100;
        MaxWidth = 350;
        MaxHeight = 100;
        Content = tracker;
        CanResize = false;

        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [ WindowTransparencyLevel.AcrylicBlur ];
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaToDecorationsHint = true;
    }

    protected async override void OnInitialized()
    {
        base.OnInitialized();
        await Task.Run(() =>
        {
            _loadingTask();
            Dispatcher.UIThread.Invoke(() => { _onComplete(); });
        });
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (!e.IsProgrammatic)
        {
            e.Cancel = true;
        }
    }
}
