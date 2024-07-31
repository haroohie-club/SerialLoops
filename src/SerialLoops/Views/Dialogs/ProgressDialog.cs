using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using SerialLoops.Controls;

namespace SerialLoops.Views.Dialogs
{
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
            Title = title;
            MinWidth = 300;
            MinHeight = 100;
            MaxWidth = 300;
            MaxHeight = 100;
            Content = tracker;
            Topmost = true;
        }

        protected async override void OnInitialized()
        {
            base.OnInitialized();
            await Task.Run(() =>
            {
                _loadingTask();
                Dispatcher.UIThread.Invoke(() =>
                {
                    _onComplete();
                });
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
}
