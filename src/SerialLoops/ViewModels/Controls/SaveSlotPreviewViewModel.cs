using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Save;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SerialLoops.Assets;
using SerialLoops.Lib.SaveFile;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.Views.Dialogs;
using SkiaSharp;

namespace SerialLoops.ViewModels.Controls;

public class SaveSlotPreviewViewModel : ViewModelBase
{
    private SaveItem _save;
    private int _slotNum;
    public SaveSlotData SlotData { get; }

    [Reactive]
    public SKBitmap SaveSlotPreview { get; set; }
    public string SlotName => _slotNum == 3 ? Strings.Quick_Save : string.Format(Strings.File__0_, _slotNum);

    private MainWindowViewModel _window;

    public ICommand SlotEditCommand { get; }
    public ICommand SlotClearCommand { get; }

    public SaveSlotPreviewViewModel(SaveItem save, SaveSlotData slotData, int slotNum, MainWindowViewModel window)
    {
        _save = save;
        SlotData = slotData;
        _slotNum = slotNum;
        _window = window;
        SaveSlotPreview = new SaveFilePreview(SlotData, _window.OpenProject).DrawPreview();

        SlotEditCommand = ReactiveCommand.CreateFromTask(EditSlot);
        SlotClearCommand = ReactiveCommand.Create(ClearSlot);
    }

    private async Task EditSlot()
    {
        await new SaveSlotEditorDialog()
        {
            DataContext = new SaveSlotEditorDialogViewModel(_save, SlotData, _save.DisplayName, SlotName,
                _window.OpenProject, _window.Log, _window.EditorTabs),
        }.ShowDialog(_window.Window);
    }

    private void ClearSlot()
    {
        SlotData.Clear();
        _save.UnsavedChanges = true;
        _window.Log.Log($"Cleared Save File {_slotNum}.");
        SaveSlotPreview = new SaveFilePreview(SlotData, _window.OpenProject).DrawPreview();
    }
}
