using Eto.Forms;
using HaruhiChokuretsuLib.Save;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using System;
using System.IO;
using SerialLoops.Lib.SaveFile;
using SerialLoops.Utility;

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
            Title = $"Edit Save File - {Path.GetFileName(_saveLoc)}";
            Width = 600;
            Height = 390;
            Resizable = false;
            Padding = 10;

            UpdateContent();
        }

        private void UpdateContent()
        {
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
            cancelButton.Click += (sender, args) => Close();
            
            StackLayout buttonsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Items =
                {
                    saveButton,
                    cancelButton,
                }
            };

            Content = new TableLayout
            {
                Rows =
                {
                    GetSaveFiles(),
                    buttonsLayout
                },
                Spacing = new(0, 20)
            };
        }

        private TableLayout GetSaveFiles()
        {
            Button saveCommonButton = new()
            {
                Text = "Common Save Data...",
                Image = ControlGenerator.GetIcon("Edit_Save", _log)
            };
            saveCommonButton.Click += (sender, args) =>
            {
                SaveSlotEditorDialog saveSlotEditorDialog = new(
                    _log,
                    _save.CommonData,
                    Path.GetFileName(_saveLoc),
                    "Common Save Data",
                    _project,
                    _tabs,
                    UpdateContent
                );
                saveSlotEditorDialog.Show();
            };
            StackLayout commonDataRow = new(saveCommonButton) { HorizontalContentAlignment = HorizontalAlignment.Center };

            return new TableLayout
            {
                Rows =
                {
                    new GroupBox
                    {
                        Text = "Save Files",
                        Padding = 5,
                        Content = new TableLayout
                        {
                            Spacing = new(0, 10),
                            Rows =
                            {
                                GetSaveSlotPreview(_save.CheckpointSaveSlots[0], 1),
                                GetSaveSlotPreview(_save.CheckpointSaveSlots[1], 2),
                                GetSaveSlotPreview(_save.QuickSaveSlot, 3),
                            },
                        }
                    },
                    commonDataRow,
                },
                Spacing = new(0, 15)
            };
        }

        private TableRow GetSaveSlotPreview(SaveSlotData data, int slotNum)
        {
            return new TableLayout
            {
                Size = new(430, 64),
                Spacing = new(6, 6),
                Rows =
                {
                    new TableRow(
                        new SKGuiImage(
                            new SaveFilePreview(data, _project).DrawPreview()
                        ),
                        new StackLayout(
                            slotNum == 3 ? "Quick Save" : $"File {slotNum}",
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 5,
                                Items =
                                {
                                    GetSlotEditButton(data, Path.GetFileName(_saveLoc), slotNum),
                                    GetSlotClearButton(data, Path.GetFileName(_saveLoc), slotNum),
                                },
                            }
                        )
                        {
                            Orientation = Orientation.Vertical,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Spacing = 5,
                        }
                    )
                }
            };
        }

        private Button GetSlotEditButton(SaveSlotData slot, string fileName, int slotNumber)
        {
            Button slotButton = new() { Width = 22, Image = ControlGenerator.GetIcon("Edit_Save", _log) };
            slotButton.Click += (sender, args) =>
            {
                SaveSlotEditorDialog saveSlotEditorDialog;
                if (slot is QuickSaveSlotData quickSave)
                {
                    saveSlotEditorDialog = new(_log, quickSave, fileName, "Quick Save Data", _project, _tabs, UpdateContent);
                }
                else
                {
                    saveSlotEditorDialog = new(_log, slot, fileName, $"File {slotNumber}", _project, _tabs, UpdateContent);
                }
                saveSlotEditorDialog.Show();
            };
            return slotButton;
        }

        private Button GetSlotClearButton(SaveSlotData slot, string fileName, int slotNumber)
        {
            Button slotButton = new()
            {
                Width = 22,
                Image = ControlGenerator.GetIcon("Clear", _log),
                Enabled = slot.EpisodeNumber > 0
            };
            slotButton.Click += (sender, args) =>
            {
                slot.Clear();
                _log.Log($"Cleared Save File {slotNumber}.");
                UpdateContent();
            };
            return slotButton;
        }
    }
}
