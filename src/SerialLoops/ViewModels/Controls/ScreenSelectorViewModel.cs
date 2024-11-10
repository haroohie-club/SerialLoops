using System;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using static SerialLoops.Lib.Script.Parameters.ScreenScriptParameter;

namespace SerialLoops.ViewModels.Controls;

public class ScreenSelectorViewModel : ViewModelBase
{
    public event EventHandler ScreenChanged;

    public bool AllowSelectingBoth { get; set; }

    private DsScreen _selectedScreen;
    public DsScreen SelectedScreen
    {
        get => _selectedScreen;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedScreen, value);
            ScreenChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ICommand SelectTopCommand { get; set; }
    public ICommand SelectBottomCommand { get; set; }
    public ICommand SelectBothCommand { get; set; }

    public ScreenSelectorViewModel(DsScreen selectedScreen, bool allowSelectingBoth)
    {
        _selectedScreen = selectedScreen;
        AllowSelectingBoth = allowSelectingBoth;

        SelectTopCommand = ReactiveCommand.Create(() => SelectedScreen = DsScreen.TOP);
        SelectBottomCommand = ReactiveCommand.Create(() => SelectedScreen = DsScreen.BOTTOM);
        SelectBothCommand = ReactiveCommand.Create<bool>((bothChecked) => SelectedScreen = bothChecked ? (SelectedScreen == DsScreen.BOTH ? DsScreen.BOTTOM : SelectedScreen) : DsScreen.BOTH); // reverse as IsChecked hasn't changed yet
    }
}