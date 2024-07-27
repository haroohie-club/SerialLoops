using Avalonia.Controls;
using SerialLoops.Assets;
using SerialLoops.Utility;
using SerialLoops.ViewModels;
using System;
using System.Diagnostics;

namespace SerialLoops.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;
        public bool RestartOnClose { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        public override void Show()
        {
            base.Show();
            _viewModel = (MainWindowViewModel)DataContext;
            _viewModel.Initialize(this);
            NativeMenu.SetMenu(this, GetInitialMenu(NativeMenu.GetMenu(this)));
        }

        private NativeMenu GetInitialMenu(NativeMenu menu)
        {
            menu.Items.Clear();
            menu.Items.Add(new NativeMenuItem()
            {
                Header = Strings._File,
                Menu =
                [
                    new NativeMenuItem()
                    {
                        Header = Strings.New_Project___,
                        Icon = ControlGenerator.GetIcon("New", _viewModel.Log),
                    },
                    new NativeMenuItem()
                    {
                        Header = Strings.Open_Project,
                        Icon = ControlGenerator.GetIcon("Open", _viewModel.Log),
                    },
                    _viewModel.RecentProjectsMenu,
                    new NativeMenuItemSeparator(),
                    new NativeMenuItem()
                    {
                        Header = Strings.Edit_Save_File,
                        Icon = ControlGenerator.GetIcon("Edit_Save", _viewModel.Log),
                    }
                ]
            });
            menu.Items.Add(new NativeMenuItem()
            {
                Header = Strings._Help,
                Menu =
                [
                    new NativeMenuItem()
                    {
                        Header = Strings.About___,
                        Icon = ControlGenerator.GetIcon("Help", _viewModel.Log),
                        Command = _viewModel.AboutCommand,
                    },
                ]
            });
            return menu;
        }

        private async void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            await _viewModel.CloseProject_Executed(e);
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