using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.Views.Panels;

public partial class HomePanel : UserControl
{
    public bool IsAppStartup { get; init; }

    public HomePanel()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        if (OperatingSystem.IsMacOS() && IsAppStartup)
        {
            await Task.Delay(500); // Give time for the URL event to be handled lol
            ((HomePanelViewModel)DataContext)!.MainWindow.HandleFilesAndPreviousProjects();
        }
        base.OnLoaded(e);
    }
}
