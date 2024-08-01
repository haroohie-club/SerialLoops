using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using SerialLoops.ViewModels;
using SerialLoops.Views;

namespace SerialLoops
{
    public partial class App : Application
    {
        private IClassicDesktopStyleApplicationLifetime _desktop;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _desktop = desktop;
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void About_Click(object? sender, EventArgs e)
        {
            ((MainWindow)_desktop.MainWindow).ViewModel.AboutCommand.Execute(Unit.Default);
        }

        private void Preferences_Click(object? sender, EventArgs e)
        {
            ((MainWindow)_desktop.MainWindow).ViewModel.PreferencesCommand.Execute(Unit.Default);
        }

        private void Updates_Click(object? sender, EventArgs e)
        {
            ((MainWindow)_desktop.MainWindow).ViewModel.CheckForUpdatesCommand.Execute(Unit.Default);
        }

        private void Logs_Click(object? sender, EventArgs e)
        {
            ((MainWindow)_desktop.MainWindow).ViewModel.ViewLogsCommand.Execute(Unit.Default);
        }
    }
}
