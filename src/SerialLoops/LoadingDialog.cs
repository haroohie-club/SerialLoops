using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Controls;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using System;
using System.Threading.Tasks;

namespace SerialLoops
{
    public class LoadingDialog : Dialog
    {

        private readonly Action _loadingTask;
        private readonly LoopyProgressTracker _tracker;
        private readonly Action _onComplete;

        public LoadingDialog(Action loadingTask, Action onComplete, LoopyProgressTracker tracker)
        {
            _loadingTask = loadingTask;
            _tracker = tracker;
            _onComplete = onComplete;
            
            Title = "Loading";
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
