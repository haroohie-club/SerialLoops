using Avalonia.Controls;
using Avalonia.Threading;
using SerialLoops.Assets;
using SerialLoops.Lib.Util;

namespace SerialLoops.Controls
{
    public class LoopyProgressTracker : StackPanel, IProgressTracker
    {
        private readonly ProgressBar _loadingProgress;
        private readonly TextBlock _loadingItem;
        private readonly string _processVerb;

        public LoopyProgressTracker(string processVerb = "")
        {
            if (string.IsNullOrEmpty(processVerb))
            {
                processVerb = Strings.Loading_;
            }

            _loadingProgress = new() { Width = 390 };
            _loadingItem = new();
            Orientation = Avalonia.Layout.Orientation.Vertical;
            Margin = new(10);
            Spacing = 10;
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            Children.Add(_loadingItem);
            Children.Add(_loadingProgress);
        }

        private int _current;
        public int Finished
        {
            get => _current;
            set
            {
                _current = value;
                Dispatcher.UIThread.Invoke(() => _loadingProgress.Value = _current);
            }
        }

        private int _total;
        public int Total
        {
            get => _total;
            set
            {
                _total = value;
                Dispatcher.UIThread.Invoke(() => _loadingProgress.Maximum = _total);
            }
        }

        private string _currentlyLoading;
        public string CurrentlyLoading
        {
            get => _currentlyLoading;
            set
            {
                _currentlyLoading = $"{_processVerb} {value}...";
                Dispatcher.UIThread.Invoke(() => _loadingItem.Text = _currentlyLoading);
            }
        }
    }
}
