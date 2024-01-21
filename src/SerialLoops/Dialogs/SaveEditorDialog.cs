using Eto.Forms;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using System;
using System.IO;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class SaveEditorDialog : Dialog
    {
        private ILogger _log;
        private string _saveLoc;
        private SaveFile _save;
        private Project _project;

        public bool LoadedSuccessfully = true;

        public SaveEditorDialog(ILogger log, Project project, string saveLoc)
        {
            _log = log;
            _saveLoc = saveLoc;
            _project = project;
            try
            {
                _save = new(File.ReadAllBytes(saveLoc));
            }
            catch (Exception ex)
            {
                _log.LogException("Error reading saving file.", ex);
                LoadedSuccessfully = false;
            }
            if (LoadedSuccessfully)
            {
                InitializeComponent();
            }
        }

        void InitializeComponent()
        {
            Width = 400;
            Height = 400;
            Button saveCommonButton = new() { Text = "Common Save Data", Height = 64 };
            saveCommonButton.Click += (sender, args) =>
            {
                SaveSlotEditorDialog saveSlotEditorDialog = new(_log, _save.CommonData, _project);
                saveSlotEditorDialog.ShowModal();
            };

            Button saveButton = new() { Text = "Save" };
            saveButton.Click += (sender, args) =>
            {
                _log.Log("Attempting to save Chokuretsu save file...");
                try
                {
                    File.WriteAllBytes(_saveLoc, _save.GetBytes());
                    Close();
                }
                catch (Exception ex)
                {
                    _log.LogException("Failed to save Chokuretsu save file!", ex);
                }
            };

            Button cancelButton = new() { Text = "Cancel" };
            cancelButton.Click += (sender, args) =>
            {
                Close();
            };

            StackLayout buttonsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items =
                {
                    saveButton,
                    cancelButton,
                }
            };

            Content = new TableLayout
            {
                Spacing = new(5, 5),
                Rows =
                {
                    new(saveCommonButton),
                    new(GetSaveSlotPreviewButton(_save.StaticSaveSlots[0])),
                    new(GetSaveSlotPreviewButton(_save.StaticSaveSlots[1])),
                    new(GetSaveSlotPreviewButton(_save.DynamicSaveSlot)),
                    new(),
                    new(buttonsLayout),
                },
            };
        }

        private Button GetSaveSlotPreviewButton(SaveSlotData slot)
        {
            Button slotButton = new() { Text = $"EPISODE: {slot.EpisodeNumber}\n\n{slot.SaveTime:yyyy/MM/dd H:mm:ss}", Height = 64 };
            slotButton.Click += (sender, args) =>
            {
                SaveSlotEditorDialog saveSlotEditorDialog;
                if (slot is DynamicSaveSlotData dynamicSave)
                {
                    saveSlotEditorDialog = new(_log, dynamicSave, _project);
                }
                else
                {
                    saveSlotEditorDialog = new(_log, slot, _project);
                }
                saveSlotEditorDialog.ShowModal();
            };
            return slotButton;
        }
    }
}
