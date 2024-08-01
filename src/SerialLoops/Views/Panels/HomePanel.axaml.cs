using System;
using System.Reactive;
using Avalonia.Controls;
using SerialLoops.ViewModels.Panels;

namespace SerialLoops.Views.Panels
{
    public partial class HomePanel : UserControl
    {
        public HomePanelViewModel ViewModel;

        public HomePanel()
        {
            InitializeComponent();
        }

        private void NewProject_PointerPressed(object? sender, EventArgs e)
        {
            ViewModel.MainWindow.NewProjectCommand.Execute(Unit.Default);
        }

        private void OpenProject_PointerPressed(object? sender, EventArgs e)
        {
            ViewModel.MainWindow.OpenProjectCommand.Execute(Unit.Default);
        }

        private void EditSaveFile_PointerPressed(object? sender, EventArgs e)
        {
            ViewModel.MainWindow.EditSaveCommand.Execute(Unit.Default);
        }

        private void Preferences_PointerPressed(object? sender, EventArgs e)
        {
            ViewModel.MainWindow.PreferencesCommand.Execute(Unit.Default);
        }

        private void About_PointerPressed(object? sender, EventArgs e)
        {
            ViewModel.MainWindow.AboutCommand.Execute(Unit.Default);
        }
    }
}
