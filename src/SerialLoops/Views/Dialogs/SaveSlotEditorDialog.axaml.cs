using Avalonia.Controls;
using Avalonia.Input;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs;

public partial class SaveSlotEditorDialog : Window
{
    public SaveSlotEditorDialog()
    {
        InitializeComponent();
    }

    private void CharacterPortraitTab_OnGotFocus(object sender, GotFocusEventArgs e)
    {
        ((SaveSlotEditorDialogViewModel)DataContext)?.LoadCharacterPortraits();
    }
}

