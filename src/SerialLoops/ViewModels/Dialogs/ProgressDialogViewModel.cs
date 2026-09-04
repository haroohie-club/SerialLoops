using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SerialLoops.Assets;
using SerialLoops.Lib.Util;

namespace SerialLoops.ViewModels.Dialogs;

public partial class ProgressDialogViewModel : ViewModelBase, IProgressTracker
{
    private Action _loadingTask;
    private Action _onComplete;

    [Reactive]
    public partial string Title { get; set; }

    [Reactive]
    public partial string ProcessVerb { get; set; }
    [Reactive]
    public partial string CurrentlyLoading { get; set; }
    [Reactive]
    public partial int Finished { get; set; }
    [Reactive]
    public partial int Total { get; set; }

    public ICommand InitializedCommand { get; }

    public ProgressDialogViewModel(string title, string processVerb = "")
    {
        Title = title;
        InitializedCommand = ReactiveCommand.CreateFromTask(OnLoaded);

        if (string.IsNullOrEmpty(processVerb))
        {
            processVerb = Strings.ProjectLoadProgressPrefix;
        }
        ProcessVerb = processVerb;
    }

    public void InitializeTasks(Action loadingTask, Action onComplete)
    {
        _loadingTask = loadingTask;
        _onComplete = onComplete;
    }

    public async Task OnLoaded()
    {
        await Task.Run(() =>
        {
            _loadingTask();
            Dispatcher.UIThread.Invoke(() => { _onComplete(); });
        });
    }
}
