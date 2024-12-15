using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.SaveFile;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Editors;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Panels;

public class SaveEditorPanelViewModel : EditorViewModel
{
    public SaveItem Save { get; }
    public SaveSlotPreviewViewModel Slot1ViewModel => new(Save, Save.Save.CheckpointSaveSlots[0], 1, Window);
    public SaveSlotPreviewViewModel Slot2ViewModel => new(Save, Save.Save.CheckpointSaveSlots[1], 2, Window);
    public SaveSlotPreviewViewModel QuickSaveViewModel => new(Save, Save.Save.QuickSaveSlot, 3, Window);

    public ICommand EditCommonSaveDataCommand { get; }

    public SaveEditorPanelViewModel(SaveItem save, MainWindowViewModel window, ILogger log, EditorTabsPanelViewModel tabs = null) :
        base(save, window, log, tabs: tabs)
    {
        Save = save;

        EditCommonSaveDataCommand = ReactiveCommand.CreateFromTask(EditCommonData);
    }

    private async Task EditCommonData()
    {
        await new SaveSlotEditorDialog()
        {
            DataContext = new SaveSlotEditorDialogViewModel(Save, Save.Save.CommonData, Save.DisplayName,
                Strings.Common_Save_Data, Window.OpenProject, _log, _tabs),
        }.ShowDialog(Window.Window);
    }
}
