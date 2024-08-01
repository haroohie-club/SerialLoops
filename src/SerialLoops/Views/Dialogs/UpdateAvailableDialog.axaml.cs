using System;
using Avalonia.Controls;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs
{
    public partial class UpdateAvailableDialog : Window
    {
        public UpdateAvailableDialogViewModel ViewModel { get; set; }

        public UpdateAvailableDialog()
        {
            InitializeComponent();
        }

        private void ReleaseLink_Click(object? sender, EventArgs e)
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
