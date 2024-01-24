using Eto.Forms;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using System;
using System.IO;
using System.Linq;

namespace SerialLoops.Dialogs
{
    public class SaveEditorDialog : FloatingForm
    {
        private ILogger _log;
        private string _saveLoc;
        private SaveFile _save;
        private Project _project;
        private EditorTabsPanel _tabs;

        public bool LoadedSuccessfully = true;

        public SaveEditorDialog(ILogger log, Project project, EditorTabsPanel tabs, string saveLoc)
        {
            _log = log;
            _saveLoc = saveLoc;
            _project = project;
            _tabs = tabs;
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
            Title = $"Save Editor - {Path.GetFileName(_saveLoc)}";
            Width = 400;
            Height = 400;
            Button saveCommonButton = new() { Text = "Common Save Data", Height = 64 };
            saveCommonButton.Click += (sender, args) =>
            {
                SaveSlotEditorDialog saveSlotEditorDialog = new(_log, _save.CommonData, _project, _tabs);
                saveSlotEditorDialog.Show();
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
                    new(GetSaveSlotPreviewButton(_save.CheckpointSaveSlots[0])),
                    new(GetSaveSlotPreviewButton(_save.CheckpointSaveSlots[1])),
                    new(GetSaveSlotPreviewButton(_save.QuickSaveSlot)),
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
                if (slot is QuickSaveSlotData quickSave)
                {
                    saveSlotEditorDialog = new(_log, quickSave, _project, _tabs);
                }
                else
                {
                    saveSlotEditorDialog = new(_log, slot, _project, _tabs);
                }
                saveSlotEditorDialog.Show();
            };
            return slotButton;
        }
    }
}
