using System;
using System.Diagnostics;
using Avalonia.Controls;
using SerialLoops.Assets;
using SerialLoops.Lib.Factories;
using SerialLoops.Utility;
using SerialLoops.ViewModels;

namespace SerialLoops.Views;

public partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel;
    public bool RestartOnClose { get; set; } = false;
    public IConfigFactory ConfigurationFactory { get; set; } // This is used for testing purposes; should always be null in production

    public MainWindow()
    {
        InitializeComponent();
    }
    public override void Show()
    {
        base.Show();
        ViewModel = (MainWindowViewModel)DataContext!;
        ViewModel.Initialize(this, ConfigurationFactory);
        NativeMenu.SetMenu(this, GetInitialMenu(NativeMenu.GetMenu(this)));
    }

    private NativeMenu GetInitialMenu(NativeMenu menu)
    {
        if (!NativeMenu.GetIsNativeMenuExported(this))
        {
            menu.Items.Clear();
        }

        NativeMenuItem fileMenu = new()
        {
            Header = Strings._File,
            Menu =
            [
                new NativeMenuItem
                {
                    Header = Strings.New_Project___,
                    Icon = ControlGenerator.GetIcon("New", ViewModel.Log),
                    Command = ViewModel.NewProjectCommand,
                },
                new NativeMenuItem
                {
                    Header = Strings.Open_Project,
                    Icon = ControlGenerator.GetIcon("Open", ViewModel.Log),
                    Command = ViewModel.OpenProjectCommand,
                },
                ViewModel.RecentProjectsMenu,
                new NativeMenuItem
                {
                    Header = Strings.Import_Project,
                    Icon = ControlGenerator.GetIcon("Import_Project", ViewModel.Log),
                    Command = ViewModel.ImportProjectCommand,
                },
                new NativeMenuItemSeparator(),
                new NativeMenuItem
                {
                    Header = Strings.ProjectRenameText,
                    Icon = ControlGenerator.GetIcon("Rename_Item", ViewModel.Log),
                    Command = ViewModel.RenameProjectCommand,
                },
                new NativeMenuItem
                {
                    Header = Strings.ProjectDuplicateText,
                    Icon = ControlGenerator.GetIcon("Copy", ViewModel.Log),
                    Command = ViewModel.DuplicateProjectCommand,
                },
                new NativeMenuItem
                {
                    Header = Strings.ProjectDeleteText,
                    Icon = ControlGenerator.GetIcon("Remove", ViewModel.Log),
                    Command = ViewModel.DeleteProjectCommand,
                },
                new NativeMenuItemSeparator(),
                new NativeMenuItem
                {
                    Header = Strings.Edit_Save_File,
                    Icon = ControlGenerator.GetIcon("Edit_Save", ViewModel.Log),
                    Command = ViewModel.EditSaveCommand,
                },
            ],
        };
        menu.Items.Add(fileMenu);
        if (!NativeMenu.GetIsNativeMenuExported(this))
        {
            fileMenu.Menu.Items.Add(new NativeMenuItemSeparator());
            fileMenu.Menu.Items.Add(new NativeMenuItem
            {
                Header = Strings._Preferences___,
                Icon = ControlGenerator.GetIcon("Options", ViewModel.Log),
                Command = ViewModel.PreferencesCommand,
            });
            fileMenu.Menu.Items.Add(new NativeMenuItem
            {
                Header = Strings._Check_for_Updates___,
                Icon = ControlGenerator.GetIcon("Update", ViewModel.Log),
                Command = ViewModel.CheckForUpdatesCommand,
            });
            fileMenu.Menu.Items.Add(new NativeMenuItem
            {
                Header = Strings.View__Logs,
                Command = ViewModel.ViewLogsCommand,
            });
            fileMenu.Menu.Items.Add(new NativeMenuItem
            {
                Header = Strings.View_Crash_Log,
                Command = ViewModel.ViewCrashLogCommand,
            });

            menu.Items.Add(new NativeMenuItem
            {
                Header = Strings._Help,
                Menu =
                [
                    new NativeMenuItem
                    {
                        Header = Strings.About___,
                        Icon = ControlGenerator.GetIcon("Help", ViewModel.Log),
                        Command = ViewModel.AboutCommand,
                    },
                ]
            });
        }
        ViewModel.WindowMenu = new()
        {
            { MenuHeader.FILE, fileMenu },
        };
        return menu;
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        await ViewModel.CloseProject_Executed(e);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        if (RestartOnClose)
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true });
        }
    }
}
