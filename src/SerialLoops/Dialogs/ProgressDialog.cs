using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Utility;
using System;
using System.Threading.Tasks;

namespace SerialLoops.Dialogs
{
    public class ProgressDialog : Dialog
    {

        private readonly Action _loadingTask;
        private readonly LoopyProgressTracker _tracker;
        private readonly Action _onComplete;

        public ProgressDialog(Action loadingTask, Action onComplete, LoopyProgressTracker tracker, string title)
        {
            _loadingTask = loadingTask;
            _tracker = tracker;
            _onComplete = onComplete;

            Title = title;
            ShowInTaskbar = false;
            Closeable = false;
            Resizable = false;
            MinimumSize = new Size(300, 100);
            Content = tracker;

            ShowModal();
        }

        protected async override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await Task.Run(() =>
            {
                _loadingTask();
                Application.Instance.Invoke(() =>
                {
                    _onComplete();
                });
            });
            Close();
        }

    }
}
