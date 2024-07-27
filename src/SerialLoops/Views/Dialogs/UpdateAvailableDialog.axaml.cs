using Avalonia.Controls;
using SerialLoops.ViewModels.Dialogs;
using System;

namespace SerialLoops.Views.Dialogs
{
    public partial class UpdateAvailableDialog : Window
    {
        public UpdateAvailableDialogViewModel ViewModel { get; set; }

        public UpdateAvailableDialog()
        {
            InitializeComponent();
        }

        private void ReleaseLink_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ViewModel.OpenReleaseLink();
        }

        private void DialogClosed(object? sender, EventArgs e)
        {
            ViewModel.Config.CheckForUpdates = CheckForUpdatesBox.IsChecked ?? false;
            ViewModel.Config.PreReleaseChannel = PreReleaseChannelBox.IsChecked ?? false;
            ViewModel.Config.Save(ViewModel.Log);
        }
    }
}