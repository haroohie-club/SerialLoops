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

        private void NewProject_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ViewModel.MainWindow.NewProjectCommand_Executed();
        }

        private void OpenProject_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ViewModel.MainWindow.OpenProjectCommand_Executed();
        }

        private void EditSaveFile_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ViewModel.MainWindow.EditSaveFileCommand_Executed();
        }

        private void Preferences_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ViewModel.MainWindow.PreferencesCommand_Executed();
        }

        private void About_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ViewModel.MainWindow.AboutCommand_Executed();
        }
    }
}