using Eto.Forms;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Utility
{
    public class LoopyProgressTracker : StackLayout, IProgressTracker
    {

        private readonly ProgressBar _loadingProgress;
        private readonly Label _loadingItem;
        private readonly string _processVerb;

        public LoopyProgressTracker(string processVerb = "Loading: ")
        {
            _processVerb = processVerb;
            _loadingProgress = new() { Width = 390 };
            _loadingItem = new();

            Padding = 10;
            Spacing = 10;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            Items.Add(_loadingItem);
            Items.Add(_loadingProgress);
        }


        private int _current;
        public int Finished { 
            get => _current;
            set
            {
                _current = value;
                Application.Instance.Invoke(() => _loadingProgress.Value = _current);
            }
        }

        private int _total;
        public int Total
        {
            get => _total;
            set
            {
                _total = value;
                Application.Instance.Invoke(() => _loadingProgress.MaxValue = _total);
            }
        }

        private string _currentlyLoading;
        public string CurrentlyLoading {
            get => _currentlyLoading;
            set
            {
                _currentlyLoading = $"{_processVerb} {value}...";
                Application.Instance.Invoke(() => _loadingItem.Text = _currentlyLoading);
            }
        }

    }
}
