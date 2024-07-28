using Avalonia.Controls;
using MsBox.Avalonia;
using SerialLoops.Assets;
using SerialLoops.Utility;
using SerialLoops.ViewModels;
using System;
using System.Diagnostics;
using System.Reactive;

namespace SerialLoops.Views
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel;
        public bool RestartOnClose { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        public override void Show()
        {
            base.Show();
            ViewModel = (MainWindowViewModel)DataContext;
            ViewModel.Initialize(this);
            NativeMenu.SetMenu(this, GetInitialMenu(NativeMenu.GetMenu(this)));
        }

        private NativeMenu GetInitialMenu(NativeMenu menu)
        {
            if (!NativeMenu.GetIsNativeMenuExported(this))
            {
                menu.Items.Clear();
            }
            NativeMenuItem fileMenu = new NativeMenuItem()
            {
                Header = Strings._File,
                Menu =
                [
                    new NativeMenuItem()
                    {
                        Header = Strings.New_Project___,
                        Icon = ControlGenerator.GetIcon("New", ViewModel.Log),
                    },
                    new NativeMenuItem()
                    {
                        Header = Strings.Open_Project,
                        Icon = ControlGenerator.GetIcon("Open", ViewModel.Log),
                    },
                    ViewModel.RecentProjectsMenu,
                    new NativeMenuItemSeparator(),
                    new NativeMenuItem()
                    {
                        Header = Strings.Edit_Save_File,
                        Icon = ControlGenerator.GetIcon("Edit_Save", ViewModel.Log),
                    }
                ]
            };
            menu.Items.Add(fileMenu);
            menu.Items.Add(new NativeMenuItem()
            {
                Header = Strings._Help,
                Menu =
                [
                    new NativeMenuItem()
                    {
                        Header = Strings.About___,
                        Icon = ControlGenerator.GetIcon("Help", ViewModel.Log),
                        Command = ViewModel.AboutCommand,
                    },
                ]
            });
            return menu;
        }

        private async void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            await ViewModel.CloseProject_Executed(e);
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            if (RestartOnClose)
            {
                Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = true });
            }
        }
    }
}