using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SerialLoops.Utility;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static HaruhiChokuretsuLib.Archive.Event.EventFile;

namespace SerialLoops.Editors
{
    public class ScriptEditor(ScriptItem item, ILogger log, Project project, EditorTabsPanel tabs) : Editor(item, log, project, tabs)
    {
        private ScriptItem _script;
        private Dictionary<ScriptSection, List<ScriptItemCommand>> _commands = new();

        private TableLayout _detailsLayout = new();
        private readonly StackLayout _preview = new() { Items = { new SKGuiImage(new(256, 384)) } };
        private StackLayout _scriptProperties = new();
        private StackLayout _editorControls = new();
        private ScriptCommandListPanel _commandsPanel;
        private Button _addCommandButton;
        private Button _addSectionButton;
        private Button _deleteButton;
        private Button _clearButton;
        private CancellationTokenSource _dialogueCancellation;
        private System.Timers.Timer _dialogueRefreshTimer;
        private int _chibiHighlighted = -1;
        private ScriptCommandDropDown _currentSpeakerDropDown; // This property is used for storing the speaker dropdown to append dialogue property dropdowns to
        private ScriptCommandCheckBox _currentLoadSoundCheckBox; // This property is used for storing the speaker dropdown to append dialogue property dropdowns to
        private Action _updateOptionDropDowns;

        public override Container GetEditorPanel()
        {
            _script = (ScriptItem)Description;
            PopulateScriptCommands();
            _script.CalculateGraphEdges(_commands, _log);
            _dialogueRefreshTimer = new(500) { AutoReset = false };
            _dialogueRefreshTimer.Elapsed += DialogueRefreshTimer_Elapsed;
            return GetCommandsContainer();
        }

        public void PopulateScriptCommands(bool refresh = false)
        {
            if (refresh)
            {
                _script.Refresh(_project, _log);
            }
            _commands = _script.GetScriptCommandTree(_project, _log);
        }

        private Container GetCommandsContainer()
        {
            TableLayout layout = new() { Spacing = new Size(5, 5) };

            _commandsPanel = new(_commands, new Size(280, 185), expandItems: true, this, _log);
            ScriptCommandSectionTreeGridView treeGridView = _commandsPanel.Viewer;
            treeGridView.SelectedItemChanged += CommandsPanel_SelectedItemChanged;

            foreach (string command in treeGridView.Control.SupportedPlatformCommands)
            {
                var barCommand = EditorCommands.Find(bc => bc.ToolBarText.ToLower().Equals(command));
                if (barCommand is not null)
                {
                    treeGridView.Control.MapPlatformCommand(command, barCommand);
                }
            }

            Command applyTemplate = new() { MenuText = Application.Instance.Localize(this, "Apply Template"), ToolBarText = Application.Instance.Localize(this, "Template"), Image = ControlGenerator.GetIcon("Template", _log) };
            applyTemplate.Executed += (sender, args) =>
            {
                ScriptTemplateSelectorDialog scriptTemplateSelector = new(_project, _script.Event, _log);
                ScriptTemplate template = scriptTemplateSelector.ShowModal(this);
                if (template is not null)
                {
                    template.Apply(_script, _project);
                    _script.Refresh(_project, _log);
                    Content = GetEditorPanel();
                    UpdateTabTitle(false);
                }
            };

            EditorCommands.Add(applyTemplate);

            TableRow mainRow = new();
            mainRow.Cells.Add(new TableLayout(GetEditorButtons(treeGridView), _commandsPanel));

            _detailsLayout = new() { Spacing = new Size(5, 5) };
            _editorControls = new() { Orientation = Orientation.Horizontal };

            TabControl propertiesTabs = GetPropertiesTabs();
            if (propertiesTabs.Pages.Count > 0)
            {
                _scriptProperties = new() { Items = { GetPropertiesTabs() } };
            }

            _detailsLayout.Rows.Add(new(new TableLayout(new TableRow(_preview, _scriptProperties))));
            _detailsLayout.Rows.Add(new(new Scrollable { Content = _editorControls }));

            mainRow.Cells.Add(new(_detailsLayout));
            layout.Rows.Add(mainRow);
            return layout;
        }

        private StackLayout GetEditorButtons(ScriptCommandSectionTreeGridView treeGridView)
        {
            _addCommandButton = new()
            {
                Image = ControlGenerator.GetIcon("Add", _log),
                ToolTip = Application.Instance.Localize(this, "New Command"),
                Width = 22,
                Enabled = treeGridView.SelectedCommandTreeItem is not null,
            };
            _addCommandButton.Click += (sender, args) =>
            {
                if (treeGridView.SelectedCommandTreeItem is not null)
                {
                    DropDown verbSelecter = new()
                    {
                        SelectedIndex = 0,
                        ToolTip = Application.Instance.Localize(this, "Select Command Type")
                    };
                    foreach (string verb in CommandsAvailable.Select(command => command.Mnemonic))
                    {
                        verbSelecter.Items.Add(new ListItem { Key = verb, Text = verb });
                    }
                    verbSelecter.Items.Sort((c1, c2) => c1.Key.CompareTo(c2.Key));

                    Button createButton = new() { Text = Application.Instance.Localize(this, "Create") };
                    Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
                    Dialog dialog = new()
                    {
                        Title = Application.Instance.Localize(this, "Add Command"),
                        MinimumSize = new(250, 150),
                        Content = new TableLayout(
                            new TableRow(new StackLayout { Padding = 10, Items = { Application.Instance.Localize(this, "Command Type:"), verbSelecter } }),
                            new TableRow(new StackLayout
                            {
                                Padding = 10,
                                Spacing = 5,
                                Width = 100,
                                Orientation = Orientation.Horizontal,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                Items = { createButton, cancelButton }
                            }
                        )),
                    };

                    dialog.Shown += (o, e) =>
                    {
                        verbSelecter.Focus();
                    };

                    MessageInfoFile messInfos = _project.Dat.GetFileByName("MESSINFOS").CastTo<MessageInfoFile>();
                    cancelButton.Click += (sender, args) =>
                    {
                        dialog.Close();
                    };
                    createButton.Click += (sender, args) =>
                    {
                        ScriptCommand scriptCommand = CommandsAvailable
                                .Find(command => command.Mnemonic.Equals(verbSelecter.SelectedKey));
                        if (scriptCommand is null)
                        {
                            _log.LogError(string.Format(Application.Instance.Localize(this, "Invalid or unavailable script command selected: {0}"), verbSelecter.SelectedKey));
                            return;
                        }
                        dialog.Close();

                        try
                        {
                            ScriptCommandSectionTreeItem item = treeGridView.SelectedCommandTreeItem;
                            if (item is null) return;

                            string sectionName = item.Text;
                            if (treeGridView.SelectedCommandTreeItem.Parent is ScriptCommandSectionTreeItem parent && !parent.Text.Equals("Top"))
                            {
                                sectionName = parent.Text;
                            }

                            ScriptSection scriptSection = _script.Event.ScriptSections.Find(section => section.Name.Equals(sectionName));
                            if (scriptSection is null)
                            {
                                _log.LogError(string.Format(Application.Instance.Localize(this, "Unable to find script section: {0}"), sectionName));
                                return;
                            }

                            int index = item.Parent is not null ? ((ScriptCommandSectionTreeItem)item.Parent).IndexOf(item) : 0;
                            ScriptCommandInvocation invocation = new(scriptCommand);

                            // Some special case initialization of new commands
                            switch (Enum.Parse<CommandVerb>(scriptCommand.Mnemonic))
                            {
                                case CommandVerb.BG_DISP:
                                case CommandVerb.BG_DISP2:
                                    invocation.Parameters[0] = (short)((BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType == BgType.TEX_BG)).Id;
                                    break;

                                case CommandVerb.BG_DISPCG:
                                    invocation.Parameters[0] = (short)((BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType != BgType.KINETIC_SCREEN && ((BackgroundItem)i).BackgroundType != BgType.TEX_BG)).Id;
                                    break;

                                case CommandVerb.BG_SCROLL:
                                    invocation.Parameters[1] = 1;
                                    break;

                                case CommandVerb.BGM_PLAY:
                                    invocation.Parameters[1] = (short)BgmModeScriptParameter.BgmMode.START;
                                    invocation.Parameters[2] = 100;
                                    break;

                                case CommandVerb.CHIBI_ENTEREXIT:
                                case CommandVerb.CHIBI_EMOTE:
                                    invocation.Parameters[0] = (short)((ChibiItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Chibi)).TopScreenIndex;
                                    break;

                                case CommandVerb.DIALOGUE:
                                    invocation.Parameters[0] = (short)(_script.Event.DialogueSection.Objects.Count - 1);
                                    DialogueLine line = new(Application.Instance.Localize(this, "Replace me").GetOriginalString(_project), _script.Event);
                                    _script.Event.DialogueSection.Objects.Insert(_script.Event.DialogueSection.Objects.Count - 1, line);
                                    invocation.Parameters[6] = (short)messInfos.MessageInfos.FindIndex(i => i.Character == line.Speaker);
                                    invocation.Parameters[7] = (short)messInfos.MessageInfos.FindIndex(i => i.Character == line.Speaker);
                                    break;

                                case CommandVerb.FLAG:
                                    invocation.Parameters[0] = 1;
                                    break;

                                case CommandVerb.GOTO:
                                    invocation.Parameters[0] = _script.Event.LabelsSection.Objects.First(l => l.Id > 0).Id;
                                    break;

                                case CommandVerb.KBG_DISP:
                                    invocation.Parameters[0] = (short)((BackgroundItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Background && ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN)).Id;
                                    break;

                                case CommandVerb.LOAD_ISOMAP:
                                    invocation.Parameters[0] = (short)((MapItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Map)).Map.Index;
                                    break;

                                case CommandVerb.INVEST_START:
                                    invocation.Parameters[4] = _script.Event.LabelsSection.Objects.First(l => l.Id > 0).Id;
                                    break;

                                case CommandVerb.MODIFY_FRIENDSHIP:
                                    invocation.Parameters[0] = 2;
                                    break;

                                case CommandVerb.SCENE_GOTO:
                                case CommandVerb.SCENE_GOTO2:
                                    invocation.Parameters[0] = (short)_script.Event.ConditionalsSection.Objects.Count;
                                    _script.Event.ConditionalsSection.Objects.Add(string.Empty);
                                    break;

                                case CommandVerb.SCREEN_FADEOUT:
                                    invocation.Parameters[1] = 100;
                                    break;

                                case CommandVerb.SND_PLAY:
                                    invocation.Parameters[1] = (short)SfxModeScriptParameter.SfxMode.START;
                                    invocation.Parameters[2] = 100;
                                    invocation.Parameters[3] = -1;
                                    invocation.Parameters[4] = -1;
                                    break;

                                case CommandVerb.SELECT:
                                    invocation.Parameters[4] = -1;
                                    invocation.Parameters[5] = -1;
                                    invocation.Parameters[6] = -1;
                                    invocation.Parameters[7] = -1;
                                    break;

                                case CommandVerb.VCE_PLAY:
                                    invocation.Parameters[0] = (short)((VoicedLineItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Voice)).Index;
                                    break;

                                case CommandVerb.VGOTO:
                                    invocation.Parameters[0] = (short)_script.Event.ConditionalsSection.Objects.Count;
                                    _script.Event.ConditionalsSection.Objects.Add(string.Empty);
                                    invocation.Parameters[2] = _script.Event.LabelsSection.Objects.First(l => l.Id > 0).Id;
                                    break;
                            }

                            ScriptItemCommand command = ScriptItemCommand.FromInvocation(
                                invocation,
                                scriptSection,
                                index == -1 ? 0 : index,
                                _script.Event,
                                _project,
                                s => Application.Instance.Localize(null, s),
                                _log
                            );

                            treeGridView.AddItem(new(new(command), scriptSection, command, _script.Event, false));
                        }
                        catch (Exception ex)
                        {
                            _log.LogException("Unable to create command", ex);
                            return;
                        }
                    };

                    dialog.ShowModal(this);
                }
            };

            _addSectionButton = new()
            {
                Image = ControlGenerator.GetIcon("Add_Section", _log),
                ToolTip = Application.Instance.Localize(this, "New Section"),
                Width = 22,
                Enabled = treeGridView.SelectedCommandTreeItem is not null,
            };
            _addSectionButton.Click += (sender, args) =>
            {
                TextBox labelBox = new() { PlaceholderText = Application.Instance.Localize(this, "Section Label") };

                Button createButton = new() { Text = Application.Instance.Localize(this, "Create") };
                Button cancelButton = new() { Text = Application.Instance.Localize(this, "Cancel") };
                Dialog dialog = new()
                {
                    Title = Application.Instance.Localize(this, "Add Section"),
                    MinimumSize = new(250, 150),
                    Content = new TableLayout(
                        new TableRow(new StackLayout { Padding = 10, Items = { Application.Instance.Localize(this, "Section Name:"), ControlGenerator.GetControlWithLabel("NONE/", labelBox) } }),
                        new TableRow(new StackLayout
                        {
                            Padding = 10,
                            Spacing = 5,
                            Width = 100,
                            Orientation = Orientation.Horizontal,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            Items = { createButton, cancelButton }
                        }
                    )),
                };

                cancelButton.Click += (sender, args) =>
                {
                    dialog.Close();
                };
                createButton.Click += (sender, args) =>
                {
                    if (string.IsNullOrWhiteSpace(labelBox.Text))
                    {
                        MessageBox.Show(Application.Instance.Localize(this, "Please enter a value for the section name"), MessageBoxType.Error);
                        return;
                    }

                    dialog.Close();
                    ScriptCommandSectionEntry section = new($"NONE{labelBox.Text}", new List<ScriptCommandSectionEntry>(), _script.Event);
                    treeGridView.AddSection(new(section, null, null, _script.Event, true));

                    _updateOptionDropDowns();
                };

                dialog.ShowModal(this);
            };

            _deleteButton = new()
            {
                Image = ControlGenerator.GetIcon("Remove", _log),
                ToolTip = Application.Instance.Localize(this, "Remove Command/Section"),
                Width = 22,
                Enabled = treeGridView.SelectedCommandTreeItem is not null,
            };
            _deleteButton.Click += (sender, args) =>
            {
                if (treeGridView.SelectedCommandTreeItem is not null)
                {
                    treeGridView.DeleteItem(treeGridView.SelectedCommandTreeItem);
                    _updateOptionDropDowns();
                }
            };

            _clearButton = new()
            {
                Image = ControlGenerator.GetIcon("Clear", _log),
                ToolTip = Application.Instance.Localize(this, "Clear Script"),
                Width = 22,
            };
            _clearButton.Click += (sender, args) =>
            {
                treeGridView.Clear();
            };

            return new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Width = _commandsPanel.Width,
                Spacing = 5,
                Padding = 5,
                Items = { _addCommandButton, _addSectionButton, _deleteButton, _clearButton }
            };
        }

        private TabControl GetPropertiesTabs()
        {
            TabControl propertiesTabs = new();

            // Starting Chibis Properties
            TabPage startingChibisPage = new() { Text = Application.Instance.Localize(this, "Starting Chibis") };
            if (_script.Event.StartingChibisSection is not null)
            {
                startingChibisPage.Content = GetStartingChibisLayout(startingChibisPage);
            }
            else
            {
                startingChibisPage.Content = GetStartingChibisAddButton(startingChibisPage);
            }

            TabPage mapCharactersPage = new() { Text = Application.Instance.Localize(this, "Map Characters") };
            if (_script.Event.MapCharactersSection is not null)
            {
                mapCharactersPage.Content = GetMapCharactersLayout(mapCharactersPage);
            }
            else
            {
                mapCharactersPage.Content = GetAddMapCharactersButton(mapCharactersPage);
            }

            TabPage choicesPage = new() { Text = Application.Instance.Localize(this, "Choices") };
            choicesPage.Content = GetChoicesStackLayout(choicesPage);

            propertiesTabs.Pages.Add(startingChibisPage);
            propertiesTabs.Pages.Add(mapCharactersPage);
            propertiesTabs.Pages.Add(choicesPage);

            return propertiesTabs;
        }

        private StackLayout GetStartingChibisLayout(TabPage parent)
        {
            List<ChibiItem> allChibis = _project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi && ((ChibiItem)i).ChibiAnimations.Any(c => c.Key.Contains("_01_"))).Cast<ChibiItem>().ToList();
            List<ChibiItem> usedChibis = allChibis.Where(c => _script.Event.StartingChibisSection.Objects
                .Select(sc => sc.ChibiIndex).ToList().Contains((short)c.TopScreenIndex)).Cast<ChibiItem>().ToList();
            ListBox availableChibisBox = new();
            ListBox usedChibisBox = new();
            availableChibisBox.Items.AddRange(allChibis.Where(i => !usedChibis.Contains(i)).Select(c => new ListItem { Key = c.DisplayName, Text = c.DisplayName }));
            usedChibisBox.Items.AddRange(usedChibis.Select(c => new ListItem { Key = c.DisplayName, Text = c.DisplayName }));

            availableChibisBox.MouseDoubleClick += (o, args) =>
            {
                if (allChibis.Count == usedChibis.Count)
                {
                    return;
                }
                IListItem chibiSelected = (IListItem)availableChibisBox.SelectedValue;
                availableChibisBox.Items.Remove(chibiSelected);
                usedChibisBox.Items.Add(chibiSelected);
                _script.Event.StartingChibisSection.Objects.Insert(_script.Event.StartingChibisSection.Objects.Count - 1, new() { ChibiIndex = (short)allChibis.First(c => c.DisplayName == chibiSelected.Text).TopScreenIndex });
                UpdateTabTitle(false);
                Application.Instance.Invoke(() => UpdatePreview());
            };
            usedChibisBox.MouseDoubleClick += (o, args) =>
            {
                if (usedChibis.Count == 0)
                {
                    return;
                }
                IListItem chibiSelected = (IListItem)usedChibisBox.SelectedValue;
                usedChibisBox.Items.Remove(chibiSelected);
                availableChibisBox.Items.Add(chibiSelected);
                availableChibisBox.Items.Sort((a, b) => allChibis.First(c => c.DisplayName == a.Key).TopScreenIndex - allChibis.First(c => c.DisplayName == b.Key).TopScreenIndex);
                _script.Event.StartingChibisSection.Objects.Remove(_script.Event.StartingChibisSection.Objects
                    .First(c => c.ChibiIndex == (short)allChibis.First(c => c.DisplayName == chibiSelected.Text).TopScreenIndex));
                UpdateTabTitle(false);
                Application.Instance.Invoke(() => UpdatePreview());
            };

            Button removeButton = new() { Text = Application.Instance.Localize(this, "Remove Starting Chibis") };
            removeButton.Click += (o, args) =>
            {
                _script.Event.StartingChibisSection = null;
                _script.Event.NumSections -= 2;
                parent.Content = GetStartingChibisAddButton(parent);
                UpdateTabTitle(false);
                Application.Instance.Invoke(() => UpdatePreview());
            };

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items =
                        {
                            availableChibisBox, usedChibisBox,
                        },
                    },
                    removeButton
                },
            };
        }

        private StackLayout GetStartingChibisAddButton(TabPage parent)
        {
            Button addButton = new() { Text = Application.Instance.Localize(this, "Add Starting Chibis") };
            addButton.Click += (o, args) =>
            {
                _script.Event.StartingChibisSection = new() { Name = "STARTINGCHIBIS" };
                _script.Event.StartingChibisSection.Objects.Add(new()); // Blank chibi entry
                _script.Event.NumSections += 2;
                parent.Content = GetStartingChibisLayout(parent);
                UpdateTabTitle(false);
                Application.Instance.Invoke(() => UpdatePreview());
            };

            return new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    addButton
                }
            };
        }

        private StackLayout GetMapCharactersLayout(TabPage parent)
        {
            MapItem[] maps = _commands.Values.SelectMany(c => c).Where(c => c.Verb == CommandVerb.LOAD_ISOMAP)
                .Select(c => ((MapScriptParameter)c.Parameters[0]).Map).ToArray();

            Button refreshMapListButton = new() { Text = Application.Instance.Localize(this, "Refresh Maps List") };
            refreshMapListButton.Click += (o, args) =>
            {
                parent.Content = GetMapCharactersLayout(parent);
            };

            Button removeButton = new() { Text = Application.Instance.Localize(this, "Remove Map Characters"), AllowDrop = true };
            removeButton.Click += (o, args) =>
            {
                _script.Event.MapCharactersSection = null;
                _script.Event.NumSections -= 2;
                parent.Content = GetAddMapCharactersButton(parent);
                UpdateTabTitle(false);
                Application.Instance.Invoke(() => UpdatePreview());
            };

            if (maps.Length == 0)
            {
                return new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 5,
                    Items =
                    {
                        Application.Instance.Localize(this, "No valid maps found."),
                        refreshMapListButton,
                        removeButton,
                    }
                };
            }

            DropDown mapsDropdown = new();
            mapsDropdown.Items.AddRange(maps.Select(m => new ListItem { Key = m.Name, Text = m.Name }));
            mapsDropdown.SelectedIndex = 0;

            PixelLayout mapLayout = new() { AllowDrop = true };
            SKBitmap mapBitmap = maps[0].GetMapImage(_project.Grp, false, false);
            Icon mapImage = new SKGuiImage(mapBitmap).WithSize(mapBitmap.Width / 2, mapBitmap.Height / 2);
            mapLayout.Add(mapImage, 0, 0);

            SKPoint gridZero = maps[0].GetOrigin(_project.Grp);
            Dictionary<PointF, Point> grid = [];
            for (int y = 0; y < maps[0].Map.PathingMap.Length; y++)
            {
                for (int x = 0; x < maps[0].Map.PathingMap[y].Length; x++)
                {
                    grid.Add(new((gridZero.X - y * 16 + x * 16) / 2, (gridZero.Y + y * 8 + x * 8) / 2), new Point(x, y));
                }
            }

            mapLayout.DragOver += (obj, args) =>
            {
                args.Effects = DragEffects.Move;
            };
            mapLayout.DragDrop += (obj, args) =>
            {
                ChibiStackLayout sourceChibiLayout = (ChibiStackLayout)args.Source;
                PointF newLocation = grid.Keys.MinBy(p => p.Distance(args.Location));
                (_script.Event.MapCharactersSection.Objects[sourceChibiLayout.ChibiIndex].X, _script.Event.MapCharactersSection.Objects[sourceChibiLayout.ChibiIndex].Y)
                    = ((short)grid[newLocation].X, (short)grid[newLocation].Y);
                mapLayout.Remove(sourceChibiLayout);
                mapLayout.Add(sourceChibiLayout,
                    ((int)gridZero.X - _script.Event.MapCharactersSection.Objects[sourceChibiLayout.ChibiIndex].Y * 16 + _script.Event.MapCharactersSection.Objects[sourceChibiLayout.ChibiIndex].X * 16 - sourceChibiLayout.ChibiBitmap.Width / 2) / 2,
                    ((int)gridZero.Y + _script.Event.MapCharactersSection.Objects[sourceChibiLayout.ChibiIndex].X * 8 + _script.Event.MapCharactersSection.Objects[sourceChibiLayout.ChibiIndex].Y * 8 - sourceChibiLayout.ChibiBitmap.Height / 2 - 24) / 2);
                UpdateTabTitle(false);
            };

            StackLayout mapDetailsLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
            };

            StackLayout mapAndDetailsLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                Items =
                {
                    mapLayout,
                    mapDetailsLayout,
                }
            };

            for (int i = 0; i < _script.Event.MapCharactersSection.Objects.Count; i++)
            {
                if (_script.Event.MapCharactersSection.Objects[i].CharacterIndex == 0)
                {
                    continue;
                }

                mapLayout.Add(GetChibiStackLayout((ChibiItem)_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).ElementAt(_script.Event.MapCharactersSection.Objects[i].CharacterIndex - 1), i, mapDetailsLayout, mapLayout, out SKBitmap chibiBitmap),
                    ((int)gridZero.X - _script.Event.MapCharactersSection.Objects[i].Y * 16 + _script.Event.MapCharactersSection.Objects[i].X * 16 - chibiBitmap.Width / 2) / 2,
                    ((int)gridZero.Y + _script.Event.MapCharactersSection.Objects[i].X * 8 + _script.Event.MapCharactersSection.Objects[i].Y * 8 - chibiBitmap.Height / 2 - 24) / 2);
            }

            GraphicSelectionButton addCharacterButton = new("Add Character", _log);
            addCharacterButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Cast<ChibiItem>());
            addCharacterButton.SelectedChanged.Executed += (obj, args) =>
            {
                int index = _script.Event.MapCharactersSection.Objects.Count - 1;
                _script.Event.MapCharactersSection.Objects.Insert(index, new() { CharacterIndex = addCharacterButton.SelectedIndex + 1 });
                mapLayout.Add(GetChibiStackLayout((ChibiItem)_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).ElementAt(_script.Event.MapCharactersSection.Objects[index].CharacterIndex - 1), index, mapDetailsLayout, mapLayout, out SKBitmap chibiBitmap),
                    ((int)gridZero.X - _script.Event.MapCharactersSection.Objects[index].Y * 16 + _script.Event.MapCharactersSection.Objects[index].X * 16 - chibiBitmap.Width / 2) / 2,
                    ((int)gridZero.Y + _script.Event.MapCharactersSection.Objects[index].X * 8 + _script.Event.MapCharactersSection.Objects[index].Y * 8 - chibiBitmap.Height / 2 - 24) / 2);
                UpdateTabTitle(false);
            };

            return new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items =
                {
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items =
                        {
                            mapsDropdown,
                            refreshMapListButton,
                        },
                    },
                    mapAndDetailsLayout,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5,
                        Items =
                        {
                            addCharacterButton,
                            removeButton,
                        }
                    }
                },
            };
        }

        private ChibiStackLayout GetChibiStackLayout(ChibiItem chibi, int index, StackLayout mapDetailsLayout, PixelLayout mapLayout, out SKBitmap chibiBitmap)
        {
            chibiBitmap = chibi.ChibiAnimations.ElementAt(_script.Event.MapCharactersSection.Objects[index].FacingDirection).Value.ElementAt(0).Frame;
            Icon chibiIcon = new SKGuiImage(chibiBitmap).WithSize(chibiBitmap.Width / 2, chibiBitmap.Height / 2);
            ChibiStackLayout chibiLayout = new()
            {
                Items =
                {
                    chibiIcon
                },
                ChibiIndex = index,
                ChibiBitmap = new(chibiBitmap.Width, chibiBitmap.Height),
            };
            chibiLayout.MouseEnter += (o, args) =>
            {
                if (_chibiHighlighted >= 0)
                {
                    return;
                }
                _chibiHighlighted = chibiLayout.ChibiIndex;
                chibiLayout.BackgroundColor = Color.FromArgb(170, 170, 200, 128);
            };
            chibiLayout.MouseLeave += (o, args) =>
            {
                if (_chibiHighlighted != chibiLayout.ChibiIndex)
                {
                    return;
                }
                _chibiHighlighted = -1;
                chibiLayout.BackgroundColor = Colors.Transparent;
            };
            chibiLayout.MouseMove += (o, args) =>
            {
                if (args.Buttons != MouseButtons.Primary || _chibiHighlighted != chibiLayout.ChibiIndex)
                {
                    return;
                }

                DataObject chibiData = new();
                chibiData.SetObject(chibi, nameof(ChibiItem));
                chibiLayout.DoDragDrop(chibiData, DragEffects.Move, chibiIcon, new(8, 16));
            };
            chibiLayout.MouseDown += (o, args) =>
            {
                mapDetailsLayout.Items.Clear();
                DropDown chibisSelectorDropDown = new();
                chibisSelectorDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Select(c => new ListItem { Key = c.Name, Text = c.Name }));
                chibisSelectorDropDown.SelectedKey = chibi.Name;

                ChibiDirectionSelector facingDirectionSelector = new(chibi, chibi.ChibiEntries.First().Name[^2..], _log)
                {
                    Direction = (ChibiItem.Direction)_script.Event.MapCharactersSection.Objects[chibiLayout.ChibiIndex].FacingDirection
                };

                void chibiEventHandler(object o, EventArgs args)
                {
                    ChibiItem newChibi = (ChibiItem)_project.Items.First(i => i.Name == chibisSelectorDropDown.SelectedKey);
                    if (newChibi.ChibiAnimations.Count <= (short)facingDirectionSelector.Direction)
                    {
                        facingDirectionSelector.Direction = ChibiItem.Direction.DOWN_LEFT;
                    }
                    SKBitmap newChibiBitmap = newChibi.ChibiAnimations.ElementAt((short)facingDirectionSelector.Direction).Value.ElementAt(0).Frame;
                    Icon newChibiIcon = new SKGuiImage(newChibiBitmap).WithSize(newChibiBitmap.Width / 2, newChibiBitmap.Height / 2);
                    chibiLayout.Items.Clear();
                    chibiLayout.Items.Add(newChibiIcon);
                    _script.Event.MapCharactersSection.Objects[chibiLayout.ChibiIndex].CharacterIndex = chibisSelectorDropDown.SelectedIndex + 1;
                    _script.Event.MapCharactersSection.Objects[chibiLayout.ChibiIndex].FacingDirection = (short)facingDirectionSelector.Direction;
                    UpdateTabTitle(false);
                }

                chibisSelectorDropDown.SelectedKeyChanged += chibiEventHandler;
                facingDirectionSelector.DirectionChanged += chibiEventHandler;

                DropDown talkScriptBlockDropDown = new();
                talkScriptBlockDropDown.Items.AddRange(_script.Event.LabelsSection.Objects.Select(l => new ListItem { Key = l.Id.ToString(), Text = l.Name }));
                talkScriptBlockDropDown.SelectedKey = _script.Event.MapCharactersSection.Objects[chibiLayout.ChibiIndex].TalkScriptBlock.ToString();
                talkScriptBlockDropDown.SelectedKeyChanged += (o, args) =>
                {
                    _script.Event.MapCharactersSection.Objects[chibiLayout.ChibiIndex].TalkScriptBlock = short.Parse(talkScriptBlockDropDown.SelectedKey);
                    UpdateTabTitle(false, talkScriptBlockDropDown);
                };

                Button removeButton = new() { Text = Application.Instance.Localize(this, "Remove Chibi") };
                removeButton.Click += (o, args) =>
                {
                    _script.Event.MapCharactersSection.Objects.RemoveAt(chibiLayout.ChibiIndex);
                    foreach (ChibiStackLayout otherChibiLayout in mapLayout.Controls.Where(c => c.GetType() == typeof(ChibiStackLayout)).Cast<ChibiStackLayout>())
                    {
                        if (otherChibiLayout.ChibiIndex > chibiLayout.ChibiIndex)
                        {
                            otherChibiLayout.ChibiIndex--;
                        }
                    }
                    UpdateTabTitle(false);
                    chibiLayout.Detach();
                };

                mapDetailsLayout.Items.Add(chibisSelectorDropDown);
                mapDetailsLayout.Items.Add(facingDirectionSelector);
                mapDetailsLayout.Items.Add(talkScriptBlockDropDown);
                mapDetailsLayout.Items.Add(removeButton);
            };

            return chibiLayout;
        }

        private StackLayout GetAddMapCharactersButton(TabPage parent)
        {
            Button addButton = new() { Text = Application.Instance.Localize(this, "Add Map Characters") };
            addButton.Click += (o, args) =>
            {
                _script.Event.MapCharactersSection = new() { Name = "MAPCHARACTERS" };
                _script.Event.MapCharactersSection.Objects.Add(new()); // Blank map characters entry
                _script.Event.NumSections += 2;
                parent.Content = GetMapCharactersLayout(parent);
                UpdateTabTitle(false);
            };

            return new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Items =
                {
                    addButton
                }
            };
        }

        private StackLayout GetChoicesStackLayout(TabPage parent)
        {
            StackLayout choicesLayout = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 3,
            };

            foreach (ChoicesSectionEntry choice in _script.Event.ChoicesSection.Objects.Skip(1).SkipLast(1))
            {
                choicesLayout.Items.Add(GetChoiceLayout(choice, choicesLayout));
            }

            Button addButton = new() { Image = ControlGenerator.GetIcon("Add", _log) };
            addButton.Click += (obj, args) =>
            {
                ChoicesSectionEntry choice = new()
                {
                    Id = _script.Event.LabelsSection.Objects
                        .FirstOrDefault(l => l.Id > 0 && _script.Event.ChoicesSection.Objects.All(c => c.Id != l.Id))?.Id ?? 0,
                    Text = string.Empty,
                };
                _script.Event.ChoicesSection.Objects.Insert(_script.Event.ChoicesSection.Objects.Count - 1, choice);
                choicesLayout.Items.Insert(choicesLayout.Items.Count - 1, GetChoiceLayout(choice, choicesLayout));
                UpdateTabTitle(false);
            };
            choicesLayout.Items.Add(addButton);

            _updateOptionDropDowns = () =>
            {
                parent.Content = GetChoicesStackLayout(parent);
            };

            return choicesLayout;
        }

        private StackLayout GetChoiceLayout(ChoicesSectionEntry choice, StackLayout parent)
        {
            StackLayout choiceLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
            };

            ScriptCommandTextBox choiceTextBox = new() { Text = choice.Text.GetSubstitutedString(_project) };
            choiceTextBox.TextChanged += (obj, args) =>
            {
                if (!choiceTextBox.FireTextChanged)
                {
                    return;
                }

                choiceTextBox.FireTextChanged = false;
                choice.Text = choiceTextBox.Text.GetOriginalString(_project);
                choiceTextBox.FireTextChanged = true;
                UpdateTabTitle(false, choiceTextBox);
            };

            DropDown scriptSectionDropDown = new();
            scriptSectionDropDown.Items.Add(new ListItem { Key = "0", Text = "NONE" });
            scriptSectionDropDown.Items.AddRange(_script.Event.LabelsSection.Objects.Where(l => l.Id > 0).Select(l => new ListItem { Key = l.Id.ToString(), Text = l.Name.Replace("/", "") }));
            scriptSectionDropDown.SelectedKey = choice.Id.ToString();
            scriptSectionDropDown.SelectedKeyChanged += (obj, args) =>
            {
                short newId = short.Parse(scriptSectionDropDown.SelectedKey);
                if (_script.Event.ChoicesSection.Objects.Any(c => c.Id == newId))
                {
                    _script.Event.ChoicesSection.Objects.First(c => c.Id == newId).Id = choice.Id;
                }
                choice.Id = newId;
                _updateOptionDropDowns();
                UpdateTabTitle(false, scriptSectionDropDown);
            };

            Button removeButton = new() { Image = ControlGenerator.GetIcon("Remove", _log) };
            removeButton.Click += (obj, args) =>
            {
                _script.Event.ChoicesSection.Objects.Remove(choice);
                parent.Items.Remove(choiceLayout);
                _updateOptionDropDowns();
                UpdateTabTitle(false);
            };

            choiceLayout.Items.Add(choiceTextBox);
            choiceLayout.Items.Add(scriptSectionDropDown);
            choiceLayout.Items.Add(removeButton);

            return choiceLayout;
        }

        private void CommandsPanel_SelectedItemChanged(object sender, EventArgs e)
        {
            try
            {
                ScriptItemCommand command = ((ScriptCommandSectionEntry)((ScriptCommandSectionTreeGridView)sender).SelectedItem).Command;
                _editorControls.Items.Clear();

                _addCommandButton.Enabled = true;
                _addSectionButton.Enabled = true;
                _deleteButton.Enabled = true;

                // if we've selected a script section header
                if (command is null)
                {
                    return;
                }

                Application.Instance.Invoke(() => UpdatePreview());

                if (command.Parameters.Count == 0)
                {
                    return;
                }

                int cols = 1;

                // We're going to embed table layouts inside a table layout so we can have colspan
                TableLayout controlsTable = new()
                {
                    Spacing = new Size(5, 5)
                };
                controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());

                int currentRow = 0, currentCol = 0, currentShort = 0;
                for (int i = 0; i < command.Parameters.Count; i++)
                {
                    ScriptParameter parameter = command.Parameters[i];
                    switch (parameter.Type)
                    {
                        case ScriptParameter.ParameterType.BG:
                            BgScriptParameter bgParam = (BgScriptParameter)parameter;
                            CommandGraphicSelectionButton bgSelectionButton = new(bgParam.Background is not null ? bgParam.Background
                                : NonePreviewableGraphic.BACKGROUND, _tabs, _log)
                            {
                                Command = command,
                                ParameterIndex = i,
                                Project = _project,
                            };

                            // Distinguish between CGs and BGs
                            if (command.Verb == CommandVerb.BG_FADE)
                            {
                                bgSelectionButton.Items.Add(NonePreviewableGraphic.BACKGROUND);
                                switch (i)
                                {
                                    case 0:
                                        bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                            (((BackgroundItem)i).BackgroundType == BgType.TEX_BG))
                                            .Select(b => b as IPreviewableGraphic));
                                        break;
                                    case 1:
                                        bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                            ((BackgroundItem)i).BackgroundType != BgType.KINETIC_SCREEN && ((BackgroundItem)i).BackgroundType != BgType.TEX_BG)
                                            .Select(b => b as IPreviewableGraphic));
                                        break;
                                }
                            }
                            else
                            {
                                switch (command.Verb)
                                {
                                    case CommandVerb.BG_DISP:
                                    case CommandVerb.BG_DISP2:
                                        bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                            (((BackgroundItem)i).BackgroundType == BgType.TEX_BG))
                                            .Select(b => b as IPreviewableGraphic));
                                        break;

                                    case CommandVerb.BG_DISPCG:
                                        bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                            ((BackgroundItem)i).BackgroundType != BgType.KINETIC_SCREEN && ((BackgroundItem)i).BackgroundType != BgType.TEX_BG)
                                            .Select(b => b as IPreviewableGraphic));
                                        break;

                                    case CommandVerb.KBG_DISP:
                                        bgSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Background &&
                                            ((BackgroundItem)i).BackgroundType == BgType.KINETIC_SCREEN).Select(b => b as IPreviewableGraphic));
                                        break;
                                }
                            }
                            bgSelectionButton.SelectedChanged.Executed += (obj, args) => BgSelectionButton_SelectionMade(bgSelectionButton, args);

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, bgSelectionButton));
                            break;

                        case ScriptParameter.ParameterType.BG_SCROLL_DIRECTION:
                            ScriptCommandDropDown bgScrollDropDown = new() { Command = command, ParameterIndex = i };
                            bgScrollDropDown.Items.AddRange(Enum.GetNames<BgScrollDirectionScriptParameter.BgScrollDirection>().Select(i => new ListItem { Text = i, Key = i }));
                            bgScrollDropDown.SelectedKey = ((BgScrollDirectionScriptParameter)parameter).ScrollDirection.ToString();
                            bgScrollDropDown.SelectedKeyChanged += BgScrollDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, bgScrollDropDown));
                            break;

                        case ScriptParameter.ParameterType.BGM:
                            BgmScriptParameter bgmParam = (BgmScriptParameter)parameter;
                            StackLayout bgmLink = ControlGenerator.GetFileLink(bgmParam.Bgm, _tabs, _log);

                            ScriptCommandDropDown bgmDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)bgmLink.Items[1].Control };
                            bgmDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.BGM).Select(i => new ListItem { Text = i.DisplayName, Key = i.DisplayName }));
                            bgmDropDown.SelectedKey = bgmParam.Bgm.DisplayName;
                            bgmDropDown.SelectedKeyChanged += BgmDropDown_SelectedKeyChanged;

                            StackLayout bgmLayout = new()
                            {
                                Orientation = Orientation.Horizontal,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                Items =
                                {
                                    ControlGenerator.GetControlWithLabel(parameter.Name, bgmDropDown),
                                    bgmLink,
                                }
                            };

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(bgmLayout);
                            break;

                        case ScriptParameter.ParameterType.BGM_MODE:
                            ScriptCommandDropDown bgmModeDropDown = new() { Command = command, ParameterIndex = i };
                            bgmModeDropDown.Items.AddRange(Enum.GetNames<BgmModeScriptParameter.BgmMode>().Select(i => new ListItem { Text = i, Key = i }));
                            bgmModeDropDown.SelectedKey = ((BgmModeScriptParameter)parameter).Mode.ToString();
                            bgmModeDropDown.SelectedKeyChanged += BgmModeDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, bgmModeDropDown));
                            break;

                        case ScriptParameter.ParameterType.BOOL:
                            ScriptCommandCheckBox boolParameterCheckbox = new() { Command = command, ParameterIndex = i, Checked = ((BoolScriptParameter)parameter).Value };
                            if (command.Verb == CommandVerb.SND_PLAY)
                            {
                                boolParameterCheckbox.DisableableNumericSteppers = [];
                                _currentLoadSoundCheckBox = boolParameterCheckbox;
                            }
                            boolParameterCheckbox.CheckedChanged += BoolParameterCheckbox_CheckedChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, boolParameterCheckbox));
                            break;

                        case ScriptParameter.ParameterType.CHARACTER:
                            ScriptCommandDropDown dialoguePropertyDropDown = new() { Command = command, ParameterIndex = i };
                            dialoguePropertyDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Character)
                                .Select(c => new ListItem { Key = c.DisplayName, Text = c.DisplayName[4..] }));
                            dialoguePropertyDropDown.SelectedKey = ((DialoguePropertyScriptParameter)parameter).Character.Name;
                            dialoguePropertyDropDown.SelectedKeyChanged += DialoguePropertyDropDown_SelectedKeyChanged;
                            _currentSpeakerDropDown.OtherDropDowns.Add(dialoguePropertyDropDown);

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, dialoguePropertyDropDown));
                            break;

                        case ScriptParameter.ParameterType.CHESS_FILE:
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name,
                                new TextBox { Text = ((ChessFileScriptParameter)parameter).ChessFileIndex.ToString() }));
                            break;

                        case ScriptParameter.ParameterType.CHESS_PIECE:
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name,
                                new TextBox { Text = ((ChessPieceScriptParameter)parameter).ChessPiece.ToString() }));
                            break;

                        case ScriptParameter.ParameterType.CHESS_SPACE:
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name,
                                new TextBox { Text = ((ChessSpaceScriptParameter)parameter).SpaceIndex.ToString() }));
                            break;

                        case ScriptParameter.ParameterType.CHIBI:
                            ScriptCommandDropDown chibiDropDown = new() { Command = command, ParameterIndex = i };
                            chibiDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Chibi).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                            chibiDropDown.SelectedKey = ((ChibiScriptParameter)parameter).Chibi.Name;
                            chibiDropDown.SelectedKeyChanged += ChibiDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, chibiDropDown));
                            break;

                        case ScriptParameter.ParameterType.CHIBI_EMOTE:
                            ScriptCommandDropDown chibiEmoteDropDown = new() { Command = command, ParameterIndex = i };
                            chibiEmoteDropDown.Items.AddRange(Enum.GetNames<ChibiEmoteScriptParameter.ChibiEmote>().Select(i => new ListItem { Text = i, Key = i }));
                            chibiEmoteDropDown.SelectedKey = ((ChibiEmoteScriptParameter)parameter).Emote.ToString();
                            chibiEmoteDropDown.SelectedKeyChanged += ChibiEmoteDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, chibiEmoteDropDown));
                            break;

                        case ScriptParameter.ParameterType.CHIBI_ENTER_EXIT:
                            ScriptCommandDropDown chibiEnterExitDropDown = new() { Command = command, ParameterIndex = i };
                            chibiEnterExitDropDown.Items.AddRange(Enum.GetNames<ChibiEnterExitScriptParameter.ChibiEnterExitType>().Select(i => new ListItem { Text = i, Key = i }));
                            chibiEnterExitDropDown.SelectedKey = ((ChibiEnterExitScriptParameter)parameter).Mode.ToString();
                            chibiEnterExitDropDown.SelectedKeyChanged += ChibiEnterExitDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, chibiEnterExitDropDown));
                            break;

                        case ScriptParameter.ParameterType.COLOR:
                            ScriptCommandColorPicker colorPicker = new()
                            {
                                AllowAlpha = false,
                                Value = ((ColorScriptParameter)parameter).Color.ToEtoDrawingColor(),
                                Command = command,
                                ParameterIndex = i,
                            };
                            colorPicker.ValueChanged += ColorPicker_ValueChanged;
                            currentShort += 2;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, colorPicker));
                            break;

                        case ScriptParameter.ParameterType.CONDITIONAL:
                            ScriptCommandTextBox conditionalBox = new() { Text = ((ConditionalScriptParameter)parameter).Conditional, Command = command, ParameterIndex = i };
                            conditionalBox.TextChanged += ConditionalBox_TextChanged;
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name,
                                conditionalBox));
                            break;

                        case ScriptParameter.ParameterType.COLOR_MONOCHROME:
                            ScriptCommandDropDown colorMonochromeDropDown = new() { Command = command, ParameterIndex = i, CurrentShort = currentShort };
                            colorMonochromeDropDown.Items.AddRange(Enum.GetNames<ColorMonochromeScriptParameter.ColorMonochrome>().Select(i => new ListItem { Text = i, Key = i }));
                            colorMonochromeDropDown.SelectedKey = ((ColorMonochromeScriptParameter)parameter).ColorType.ToString();
                            colorMonochromeDropDown.SelectedKeyChanged += ColorMonochromeDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, colorMonochromeDropDown));
                            break;

                        case ScriptParameter.ParameterType.DIALOGUE:
                            DialogueScriptParameter dialogueParam = (DialogueScriptParameter)parameter;
                            ScriptCommandDropDown speakerDropDown = new() { Command = command, ParameterIndex = i, OtherDropDowns = [] };
                            speakerDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Character).Select(c => new ListItem { Key = c.DisplayName, Text = c.DisplayName[4..] }));
                            try
                            {
                                speakerDropDown.SelectedKey = _project.Items.First(i => i.Type == ItemDescription.ItemType.Character && i.DisplayName == $"CHR_{_project.Characters[(int)dialogueParam.Line.Speaker].Name}").DisplayName;
                            }
                            catch (InvalidOperationException)
                            {
                                _log.LogError(Application.Instance.Localize(this, "Failed to find character item -- have you saved all of your changes to character names?"));
                                return;
                            }

                            speakerDropDown.SelectedKeyChanged += SpeakerDropDown_SelectedKeyChanged;
                            if (currentCol > 0)
                            {
                                controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                                currentRow++;
                                currentCol = 0;
                            }
                            _currentSpeakerDropDown = speakerDropDown;

                            ScriptCommandTextArea dialogueTextArea = new()
                            {
                                Text = dialogueParam.Line.Text.GetSubstitutedString(_project),
                                AcceptsReturn = true,
                                Command = command,
                                ParameterIndex = i,
                            };
                            dialogueTextArea.TextChanged += DialogueTextArea_TextChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabelTable(parameter.Name,
                                new StackLayout
                                {
                                    Orientation = Orientation.Horizontal,
                                    Items =
                                    {
                                    speakerDropDown,
                                    new StackLayoutItem(dialogueTextArea, expand: true),
                                    },
                                }));
                            controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());
                            currentRow++;
                            currentCol = 0;
                            break;

                        case ScriptParameter.ParameterType.EPISODE_HEADER:
                            ScriptCommandDropDown epHeaderDropDown = new() { Command = command, ParameterIndex = i };
                            epHeaderDropDown.Items.AddRange(Enum.GetNames<EpisodeHeaderScriptParameter.Episode>().Select(e => new ListItem { Key = e, Text = e }));
                            epHeaderDropDown.SelectedKey = ((EpisodeHeaderScriptParameter)parameter).EpisodeHeaderIndex.ToString();
                            epHeaderDropDown.SelectedKeyChanged += EpHeaderDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name,
                                epHeaderDropDown));
                            break;

                        case ScriptParameter.ParameterType.FLAG:
                            ScriptCommandTextBox flagTextBox = new() { Command = command, ParameterIndex = i, Text = ((FlagScriptParameter)parameter).FlagName };
                            flagTextBox.TextChanged += FlagTextBox_TextChanged;
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, flagTextBox));
                            break;

                        case ScriptParameter.ParameterType.FRIENDSHIP_LEVEL:
                            ScriptCommandDropDown friendshipLevelDropDown = new() { Command = command, ParameterIndex = i };
                            friendshipLevelDropDown.Items.AddRange(Enum.GetNames<FriendshipLevelScriptParameter.FriendshipCharacter>().Select(f => new ListItem { Key = f, Text = f }));
                            friendshipLevelDropDown.SelectedKey = ((FriendshipLevelScriptParameter)parameter).Character.ToString();
                            friendshipLevelDropDown.SelectedIndexChanged += FriendshipLevelDropDown_SelectedIndexChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, friendshipLevelDropDown));
                            break;

                        case ScriptParameter.ParameterType.ITEM:
                            CommandGraphicSelectionButton itemSelectionButton = new(
                                (ItemItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Item && ((ItemItem)i).ItemIndex == ((ItemScriptParameter)parameter).ItemIndex),
                                _tabs,
                                _log)
                            {
                                Command = command,
                                ParameterIndex = i,
                            };
                            itemSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Item).Cast<ItemItem>());
                            itemSelectionButton.SelectedChanged.Executed += (obj, args) => ItemSelectionButton_SelectedChanged(itemSelectionButton, args);
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, itemSelectionButton));
                            break;

                        case ScriptParameter.ParameterType.ITEM_LOCATION:
                            ScriptCommandDropDown itemLocationDropDown = new() { Command = command, ParameterIndex = i };
                            itemLocationDropDown.Items.AddRange(Enum.GetNames<ItemItem.ItemLocation>().Select(l => new ListItem { Key = l, Text = l }));
                            itemLocationDropDown.SelectedKey = ((ItemLocationScriptParameter)parameter).Location.ToString();
                            itemLocationDropDown.SelectedKeyChanged += ItemLocationDropDown_SelectedKeyChanged;
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, itemLocationDropDown));
                            break;

                        case ScriptParameter.ParameterType.ITEM_TRANSITION:
                            ScriptCommandDropDown itemTransitionDropDown = new() { Command = command, ParameterIndex = i };
                            itemTransitionDropDown.Items.AddRange(Enum.GetNames<ItemItem.ItemTransition>().Select(l => new ListItem { Key = l, Text = l }));
                            itemTransitionDropDown.SelectedKey = ((ItemTransitionScriptParameter)parameter).Transition.ToString();
                            itemTransitionDropDown.SelectedKeyChanged += ItemTransitionDropDown_SelectedKeyChanged;
                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, itemTransitionDropDown));
                            break;

                        case ScriptParameter.ParameterType.MAP:
                            MapScriptParameter mapParam = (MapScriptParameter)parameter;
                            ScriptCommandDropDown mapDropDown = new() { Command = command, ParameterIndex = i };
                            mapDropDown.Items.Add(new ListItem { Text = "NONE", Key = "NONE" });
                            mapDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Map).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                            mapDropDown.SelectedKey = mapParam.Map?.Name ?? "NONE";
                            mapDropDown.SelectedKeyChanged += MapDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, mapDropDown));
                            break;

                        case ScriptParameter.ParameterType.OPTION:
                            OptionScriptParameter optionParam = (OptionScriptParameter)parameter;
                            ScriptCommandDropDown optionDropDown = new() { Command = command, ParameterIndex = i };
                            optionDropDown.Items.Add(new ListItem { Text = "NONE", Key = "0" });
                            optionDropDown.Items.AddRange(_script.Event.ChoicesSection.Objects.Skip(1).SkipLast(1).Select(c => new ListItem { Text = c.Text.GetSubstitutedString(_project), Key = c.Id.ToString() }));
                            optionDropDown.SelectedKey = optionParam.Option.Id.ToString();
                            optionDropDown.SelectedKeyChanged += OptionDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, optionDropDown));
                            break;

                        case ScriptParameter.ParameterType.PALETTE_EFFECT:
                            ScriptCommandDropDown paletteEffectDropDown = new() { Command = command, ParameterIndex = i };
                            paletteEffectDropDown.Items.AddRange(Enum.GetNames<PaletteEffectScriptParameter.PaletteEffect>().Select(t => new ListItem { Text = t, Key = t }));
                            paletteEffectDropDown.SelectedKey = ((PaletteEffectScriptParameter)parameter).Effect.ToString();
                            paletteEffectDropDown.SelectedKeyChanged += PaletteEffectDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, paletteEffectDropDown));
                            break;

                        case ScriptParameter.ParameterType.PLACE:
                            PlaceScriptParameter placeParam = (PlaceScriptParameter)parameter;
                            CommandGraphicSelectionButton placeSelectionButton = new(placeParam.Place is not null ? placeParam.Place
                                : NonePreviewableGraphic.PLACE, _tabs, _log)
                            {
                                Command = command,
                                ParameterIndex = i,
                                Project = _project,
                            };
                            placeSelectionButton.Items.Add(NonePreviewableGraphic.PLACE);
                            placeSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Place)
                                .Select(p => p as IPreviewableGraphic));
                            placeSelectionButton.SelectedChanged.Executed += (obj, args) => PlaceSelectionButtonSelectedChanged_Executed(placeSelectionButton, args);

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, placeSelectionButton));
                            break;

                        case ScriptParameter.ParameterType.SCREEN:
                            ScriptCommandScreenSelector screenSelector = new(_log, ((ScreenScriptParameter)parameter).Screen, true)
                            {
                                Command = command,
                                ParameterIndex = i,
                                CurrentShort = currentShort,
                            };
                            screenSelector.ScreenChanged += ScreenSelector_ScreenChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, screenSelector));
                            break;

                        case ScriptParameter.ParameterType.SCRIPT_SECTION:
                            ScriptCommandDropDown scriptSectionDropDown = new() { Command = command, ParameterIndex = i, CurrentShort = i };
                            if (command.Verb == CommandVerb.VGOTO)
                            {
                                scriptSectionDropDown.CurrentShort = 2;
                            }
                            scriptSectionDropDown.Items.AddRange(_script.Event.ScriptSections.Where(s => (_script.Event.LabelsSection.Objects.FirstOrDefault(l => l.Name.Replace("/", "") == s.Name)?.Id ?? 0) != 0).Select(s => new ListItem { Text = s.Name, Key = s.Name }));
                            scriptSectionDropDown.SelectedKey = ((ScriptSectionScriptParameter)parameter)?.Section?.Name ?? "NONE";
                            scriptSectionDropDown.SelectedKeyChanged += ScriptSectionDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, scriptSectionDropDown));
                            break;

                        case ScriptParameter.ParameterType.SFX:
                            SfxScriptParameter sfxParam = (SfxScriptParameter)parameter;
                            StackLayout sfxLink = ControlGenerator.GetFileLink(sfxParam.Sfx, _tabs, _log);
                            ScriptCommandDropDown sfxDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)sfxLink.Items[1].Control };
                            sfxDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.SFX).Select(s => new ListItem { Key = s.DisplayName, Text = s.DisplayName }));
                            sfxDropDown.SelectedKey = sfxParam.Sfx.DisplayName;
                            sfxDropDown.SelectedKeyChanged += SfxDropDown_SelectedKeyChanged;

                            StackLayout sfxLayout = new()
                            {
                                Orientation = Orientation.Horizontal,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                Items =
                                {
                                    ControlGenerator.GetControlWithLabel(parameter.Name, sfxDropDown),
                                    sfxLink,
                                }
                            };

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(sfxLayout);
                            break;

                        case ScriptParameter.ParameterType.SFX_MODE:
                            ScriptCommandDropDown sfxModeDropDown = new() { Command = command, ParameterIndex = i };
                            sfxModeDropDown.Items.AddRange(Enum.GetNames<SfxModeScriptParameter.SfxMode>().Select(t => new ListItem { Text = t, Key = t }));
                            sfxModeDropDown.SelectedKey = ((SfxModeScriptParameter)parameter).Mode.ToString();
                            sfxModeDropDown.SelectedKeyChanged += SfxModeDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, sfxModeDropDown));
                            break;

                        case ScriptParameter.ParameterType.SHORT:
                            ScriptCommandNumericStepper shortNumericStepper = new()
                            {
                                Command = command,
                                ParameterIndex = i,
                                MinValue = short.MinValue,
                                MaxValue = short.MaxValue,
                                DecimalPlaces = 0,
                                Value = ((ShortScriptParameter)parameter).Value
                            };
                            if (command.Verb == CommandVerb.SND_PLAY && parameter.Name.Equals(Application.Instance.Localize(this, "Crossfade Time (Frames)"), StringComparison.OrdinalIgnoreCase))
                            {
                                _currentLoadSoundCheckBox.DisableableNumericSteppers.Add(shortNumericStepper);
                                shortNumericStepper.MinValue = -1;
                                if (((ShortScriptParameter)parameter).Value < 0 && (_currentLoadSoundCheckBox.Checked ?? false))
                                {
                                    shortNumericStepper.Enabled = false;
                                }
                                else
                                {
                                    _currentLoadSoundCheckBox.Checked = false;
                                }
                            }
                            else if (parameter.Name.Contains(Application.Instance.Localize(this, "Frames")))
                            {
                                shortNumericStepper.MinValue = 0;
                            }
                            if (parameter.Name.Contains(Application.Instance.Localize(this, "Volume")))
                            {
                                shortNumericStepper.MinValue = 0;
                                shortNumericStepper.MaxValue = 100;
                            }
                            if (command.Verb == CommandVerb.HARUHI_METER)
                            {
                                shortNumericStepper.ParameterIndex++;
                            }
                            shortNumericStepper.ValueChanged += ShortNumericStepper_ValueChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, shortNumericStepper));
                            break;

                        case ScriptParameter.ParameterType.SPRITE:
                            SpriteScriptParameter spriteParam = (SpriteScriptParameter)parameter;
                            CommandGraphicSelectionButton spriteSelectionButton = new(spriteParam.Sprite is not null ? spriteParam.Sprite
                                : NonePreviewableGraphic.CHARACTER_SPRITE, _tabs, _log)
                            {
                                Command = command,
                                ParameterIndex = i,
                                Project = _project,
                            };
                            spriteSelectionButton.Items.Add(NonePreviewableGraphic.CHARACTER_SPRITE);
                            spriteSelectionButton.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Character_Sprite).Select(s => (IPreviewableGraphic)s));
                            spriteSelectionButton.SelectedChanged.Executed += (obj, args) => SpriteSelectionButton_SelectionMade(spriteSelectionButton, args);

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, spriteSelectionButton));
                            break;

                        case ScriptParameter.ParameterType.SPRITE_ENTRANCE:
                            ScriptCommandDropDown spriteEntranceDropDown = new() { Command = command, ParameterIndex = i };
                            spriteEntranceDropDown.Items.AddRange(Enum.GetNames<SpriteEntranceScriptParameter.SpriteEntranceTransition>().Select(t => new ListItem { Text = t, Key = t }));
                            spriteEntranceDropDown.SelectedKey = ((SpriteEntranceScriptParameter)parameter).EntranceTransition.ToString();
                            spriteEntranceDropDown.SelectedKeyChanged += SpriteEntranceDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, spriteEntranceDropDown));
                            break;

                        case ScriptParameter.ParameterType.SPRITE_EXIT:
                            ScriptCommandDropDown spriteExitDropDown = new() { Command = command, ParameterIndex = i };
                            spriteExitDropDown.Items.AddRange(Enum.GetNames<SpriteExitScriptParameter.SpriteExitTransition>().Select(t => new ListItem { Text = t, Key = t }));
                            spriteExitDropDown.SelectedKey = ((SpriteExitScriptParameter)parameter).ExitTransition.ToString();
                            spriteExitDropDown.SelectedKeyChanged += SpriteExitDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, spriteExitDropDown));
                            break;

                        case ScriptParameter.ParameterType.SPRITE_SHAKE:
                            ScriptCommandDropDown spriteShakeDropDown = new() { Command = command, ParameterIndex = i };
                            spriteShakeDropDown.Items.AddRange(Enum.GetNames<SpriteShakeScriptParameter.SpriteShakeEffect>().Select(t => new ListItem { Text = t, Key = t }));
                            spriteShakeDropDown.SelectedKey = ((SpriteShakeScriptParameter)parameter).ShakeEffect.ToString();
                            spriteShakeDropDown.SelectedKeyChanged += SpriteShakeDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, spriteShakeDropDown));
                            break;

                        case ScriptParameter.ParameterType.TEXT_ENTRANCE_EFFECT:
                            ScriptCommandDropDown textEntranceEffectDropDown = new() { Command = command, ParameterIndex = i };
                            textEntranceEffectDropDown.Items.AddRange(Enum.GetNames<TextEntranceEffectScriptParameter.TextEntranceEffect>().Select(t => new ListItem { Text = t, Key = t }));
                            textEntranceEffectDropDown.SelectedKey = ((TextEntranceEffectScriptParameter)parameter).EntranceEffect.ToString();
                            textEntranceEffectDropDown.SelectedKeyChanged += TextEntranceEffectDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, textEntranceEffectDropDown));
                            break;

                        case ScriptParameter.ParameterType.TOPIC:
                            string topicName = _project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic &&
                                ((TopicItem)i).TopicEntry.Id == ((TopicScriptParameter)parameter).TopicId)?.DisplayName;
                            if (string.IsNullOrEmpty(topicName))
                            {
                                // If the topic has been deleted, we will just display the index in a textbox
                                TopicSelectButton setUpTopicControlButton = new() { Text = Application.Instance.Localize(this, "Select a Topic"), ScriptCommand = command, ParameterIndex = i };

                                StackLayout deletedTopicLayout = new()
                                {
                                    Orientation = Orientation.Horizontal,
                                    Items =
                                    {
                                        new TextBox { Text = ((TopicScriptParameter)parameter).TopicId.ToString() },
                                        setUpTopicControlButton,
                                    },
                                };

                                setUpTopicControlButton.Layout = deletedTopicLayout;
                                setUpTopicControlButton.Click += SetUpTopicControlButton_Click;

                                ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                    ControlGenerator.GetControlWithLabel(parameter.Name,
                                    deletedTopicLayout));
                            }
                            else
                            {
                                StackLayout topicLink = ControlGenerator.GetFileLink(_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic &&
                                    ((TopicItem)i).TopicEntry.Id == ((TopicScriptParameter)parameter).TopicId), _tabs, _log);

                                ScriptCommandDropDown topicDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)topicLink.Items[1].Control };
                                topicDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Topic)
                                    .Select(t => new ListItem { Key = t.DisplayName, Text = t.DisplayName }));
                                topicDropDown.SelectedKey = topicName;
                                topicDropDown.SelectedIndexChanged += TopicDropDown_SelectedIndexChanged;

                                StackLayout topicLinkLayout = new()
                                {
                                    Orientation = Orientation.Horizontal,
                                    Items =
                                    {
                                        topicDropDown,
                                        topicLink,
                                    },
                                };

                                ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                    ControlGenerator.GetControlWithLabel(parameter.Name,
                                    topicLinkLayout));
                            }
                            break;

                        case ScriptParameter.ParameterType.TRANSITION:
                            ScriptCommandDropDown transitionDropDown = new() { Command = command, ParameterIndex = i };
                            transitionDropDown.Items.AddRange(Enum.GetNames<TransitionScriptParameter.TransitionEffect>().Select(t => new ListItem { Text = t, Key = t }));
                            transitionDropDown.SelectedKey = ((TransitionScriptParameter)parameter).Transition.ToString();
                            transitionDropDown.SelectedKeyChanged += TransitionDropDown_SelectedKeyChanged;

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(
                                ControlGenerator.GetControlWithLabel(parameter.Name, transitionDropDown));
                            break;

                        case ScriptParameter.ParameterType.VOICE_LINE:
                            VoicedLineScriptParameter vceParam = (VoicedLineScriptParameter)parameter;
                            StackLayout vceLink = ControlGenerator.GetFileLink(vceParam.VoiceLine is not null ? vceParam.VoiceLine : NoneItem.VOICE, _tabs, _log);

                            ScriptCommandDropDown vceDropDown = new() { Command = command, ParameterIndex = i, Link = (ClearableLinkButton)vceLink.Items[1].Control };
                            vceDropDown.Items.Add(new ListItem { Key = "NONE", Text = "NONE" });
                            vceDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Voice).Select(i => new ListItem { Text = i.Name, Key = i.Name }));
                            vceDropDown.SelectedKey = vceParam.VoiceLine?.Name ?? "NONE";
                            vceDropDown.SelectedKeyChanged += VceDropDown_SelectedKeyChanged;

                            StackLayout vceLayout = new()
                            {
                                Orientation = Orientation.Horizontal,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                Items =
                                {
                                    ControlGenerator.GetControlWithLabel(parameter.Name, vceDropDown),
                                    vceLink,
                                }
                            };

                            ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows[0].Cells.Add(vceLayout);
                            break;

                        default:
                            _log.LogError(string.Format(Application.Instance.Localize(this, "Invalid parameter detected in script {0} parameter {1}"), _script.Name, parameter.Name));
                            break;
                    }
                    currentCol++;
                    if (currentCol >= cols)
                    {
                        currentCol = 0;
                        currentRow++;
                        controlsTable.Rows.Add(new(new TableLayout { Spacing = new Size(5, 5) }));
                        ((TableLayout)controlsTable.Rows.Last().Cells[0].Control).Rows.Add(new());
                    }
                    currentShort++;
                }

                _editorControls.Items.Add(new StackLayoutItem(controlsTable, expand: true));
            }
            catch (Exception ex)
            {
                _log.LogException("Failed to load editor controls!", ex);
            }
        }

        private void UpdatePreview()
        {
            try
            {
                _preview.Items.Clear();
                ScriptItemCommand currentCommand = ((ScriptCommandSectionEntry)_commandsPanel.Viewer.SelectedItem)?.Command;
                (SKBitmap previewBitmap, string errorImage) = _script.GeneratePreviewImage(_commands, currentCommand, _project, _log);
                if (previewBitmap is null)
                {
                    previewBitmap = new(256, 384);
                    SKCanvas canvas = new(previewBitmap);
                    canvas.DrawColor(SKColors.Black);
                    using Stream noPreviewStream = Assembly.GetCallingAssembly().GetManifestResourceStream(errorImage);
                    canvas.DrawImage(SKImage.FromEncodedData(noPreviewStream), new SKPoint(0, 0));
                    canvas.Flush();
                    _preview.Items.Add(new SKGuiImage(previewBitmap));
                }
                _preview.Items.Add(new SKGuiImage(previewBitmap));
            }
            catch (Exception ex)
            {
                _log.LogException("Failed to update preview!", ex);
            }
        }

        private void BgSelectionButton_SelectionMade(object sender, EventArgs e)
        {
            CommandGraphicSelectionButton selection = (CommandGraphicSelectionButton)sender;
            _log.Log($"Attempting to modify parameter {selection.ParameterIndex} to background {((ItemDescription)selection.Selected).Name} in {selection.Command.Index} in file {_script.Name}...");
            if (((ItemDescription)selection.Selected).Name == "NONE")
            {
                ((BgScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Background =
                    (BackgroundItem)_project.Items.FirstOrDefault(i => i.Name == ((ItemDescription)selection.Selected).Name);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] = 0;
            }
            else
            {
                ((BgScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Background =
                    (BackgroundItem)_project.Items.FirstOrDefault(i => i.Name == ((ItemDescription)selection.Selected).Name);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                    (short)((BackgroundItem)_project.Items.First(i => i.Name == ((ItemDescription)selection.Selected).Name)).Id;
            }
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void BgScrollDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BG scroll direction {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((BgScrollDirectionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ScrollDirection =
                Enum.Parse<BgScrollDirectionScriptParameter.BgScrollDirection>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<BgScrollDirectionScriptParameter.BgScrollDirection>(dropDown.SelectedKey);

            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void BgmDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BGM {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            BackgroundMusicItem bgm = (BackgroundMusicItem)_project.Items.FirstOrDefault(i => i.DisplayName == dropDown.SelectedKey);
            ((BgmScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Bgm = bgm;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)((BackgroundMusicItem)_project.Items.First(i => i.DisplayName == dropDown.SelectedKey)).Index;

            dropDown.Link.Text = bgm.DisplayName;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.DisplayName == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false, dropDown);
        }
        private void BgmModeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BGM mode {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((BgmModeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Mode =
                Enum.Parse<BgmModeScriptParameter.BgmMode>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<BgmModeScriptParameter.BgmMode>(dropDown.SelectedKey);

            UpdateTabTitle(false, dropDown);
        }
        private void BoolParameterCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            ScriptCommandCheckBox checkBox = (ScriptCommandCheckBox)sender;
            _log.Log($"Attempting to modify parameter {checkBox.ParameterIndex} to {checkBox.Checked} in {checkBox.Command.Index} in file {_script.Name}...");
            ((BoolScriptParameter)checkBox.Command.Parameters[checkBox.ParameterIndex]).Value = checkBox.Checked ?? false;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(checkBox.Command.Section)]
                .Objects[checkBox.Command.Index].Parameters[checkBox.ParameterIndex] = (checkBox.Checked ?? false) ? ((BoolScriptParameter)checkBox.Command.Parameters[checkBox.ParameterIndex]).TrueValue : ((BoolScriptParameter)checkBox.Command.Parameters[checkBox.ParameterIndex]).FalseValue;
            foreach (ScriptCommandNumericStepper disableableStepper in checkBox.DisableableNumericSteppers)
            {
                disableableStepper.Value = (checkBox.Checked ?? false) ? -1 : 0;
                disableableStepper.Enabled = !(checkBox.Checked ?? false);
            }

            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ChibiDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to chibi {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ChibiScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Chibi =
                (ChibiItem)_project.Items.First(i => i.Name == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)((ChibiItem)_project.Items.First(i => i.Name == dropDown.SelectedKey)).TopScreenIndex;
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ChibiEmoteDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to chibi emote {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ChibiEmoteScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Emote =
                Enum.Parse<ChibiEmoteScriptParameter.ChibiEmote>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<ChibiEmoteScriptParameter.ChibiEmote>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ChibiEnterExitDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to chibi enter/exit {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ChibiEnterExitScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Mode =
                Enum.Parse<ChibiEnterExitScriptParameter.ChibiEnterExitType>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<ChibiEnterExitScriptParameter.ChibiEnterExitType>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ColorPicker_ValueChanged(object sender, EventArgs e)
        {
            ScriptCommandColorPicker colorPicker = (ScriptCommandColorPicker)sender;
            _log.Log($"Attempting to modify parameters {colorPicker.ParameterIndex} through {colorPicker.ParameterIndex + 2} to color #{colorPicker.Value.ToHex()} in {colorPicker.Command.Index} in file {_script.Name}...");
            SKColor color = colorPicker.Value.ToSKColor();
            ((ColorScriptParameter)colorPicker.Command.Parameters[colorPicker.ParameterIndex]).Color = color;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(colorPicker.Command.Section)]
                .Objects[colorPicker.Command.Index].Parameters[colorPicker.ParameterIndex] = (short)color.Red;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(colorPicker.Command.Section)]
                .Objects[colorPicker.Command.Index].Parameters[colorPicker.ParameterIndex + 1] = (short)color.Green;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(colorPicker.Command.Section)]
                .Objects[colorPicker.Command.Index].Parameters[colorPicker.ParameterIndex + 2] = (short)color.Blue;
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ColorMonochromeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to monochrome color {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ColorMonochromeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ColorType =
                Enum.Parse<ColorMonochromeScriptParameter.ColorMonochrome>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.CurrentShort] =
                (short)Enum.Parse<ColorMonochromeScriptParameter.ColorMonochrome>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ConditionalBox_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextBox textBox = (ScriptCommandTextBox)sender;
            _log.Log($"Attempting to modify parameter {textBox.ParameterIndex} to conditional {textBox.Text} in {textBox.Command.Index} in file {_script.Name}...");
            ((ConditionalScriptParameter)textBox.Command.Parameters[textBox.ParameterIndex]).Conditional = textBox.Text;
            if (_script.Event.ConditionalsSection.Objects.Contains(textBox.Text))
            {
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(textBox.Command.Section)]
                    .Objects[textBox.Command.Index].Parameters[textBox.ParameterIndex] = (short)_script.Event.ConditionalsSection.Objects.IndexOf(textBox.Text);
            }
            else
            {
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(textBox.Command.Section)]
                    .Objects[textBox.Command.Index].Parameters[textBox.ParameterIndex] = (short)_script.Event.ConditionalsSection.Objects.Count;
                _script.Event.ConditionalsSection.Objects.Add(textBox.Text);
            }
            UpdateTabTitle(false, textBox);
        }
        private void SpeakerDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            string originalSpeaker = _project.Characters[(int)((DialogueScriptParameter)dropDown.Command.Parameters[0]).Line.Speaker].Name;
            _log.Log($"Attempting to modify speaker in parameter {dropDown.ParameterIndex} to character {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((DialogueScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Line.Speaker =
                (Speaker)_project.Characters.FirstOrDefault(c => c.Value.Name == dropDown.SelectedValue.ToString()).Key;
            _script.Event.DialogueSection.Objects[dropDown.Command.Section.Objects[dropDown.Command.Index].Parameters[0]].Speaker =
                (Speaker)_project.Characters.FirstOrDefault(c => c.Value.Name == dropDown.SelectedValue.ToString()).Key;
            foreach (ScriptCommandDropDown otherDropDown in dropDown.OtherDropDowns)
            {
                if (otherDropDown.SelectedValue.ToString() == originalSpeaker)
                {
                    otherDropDown.SelectedKey = dropDown.SelectedKey;
                }
            }
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void DialogueTextArea_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextArea textArea = (ScriptCommandTextArea)sender;
            if (!textArea.FireTextChanged)
            {
                return;
            }

            int currentCaretIndex = textArea.CaretIndex;
            textArea.FireTextChanged = false;
            textArea.Text = Regex.Replace(textArea.Text, @"^""", "“");
            textArea.Text = Regex.Replace(textArea.Text, @"(\s)""", "$1“");
            textArea.Text = textArea.Text.Replace('"', '”');
            textArea.FireTextChanged = true;
            textArea.CaretIndex = currentCaretIndex;

            _log.Log($"Attempting to modify dialogue in parameter {textArea.ParameterIndex} to dialogue '{textArea.Text}' in {textArea.Command.Index} in file {_script.Name}...");

            _dialogueCancellation?.Cancel();
            _dialogueCancellation = new();

            string text = textArea.Text;
            ScriptItemCommand command = textArea.Command;
            int parameterIndex = textArea.ParameterIndex;
            Task task = new(() =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    ((DialogueScriptParameter)command.Parameters[parameterIndex]).Line.Text = "";
                    _script.Event.DialogueSection.Objects[command.Section.Objects[command.Index].Parameters[0]].Text = "";
                    ((DialogueScriptParameter)command.Parameters[parameterIndex]).Line.Pointer = 0;
                    _script.Event.DialogueSection.Objects[command.Section.Objects[command.Index].Parameters[0]].Pointer = 0;
                }
                else
                {
                    if (((DialogueScriptParameter)command.Parameters[parameterIndex]).Line.Pointer == 0)
                    {
                        // It doesn't matter what we set this to as long as it's greater than zero
                        // The ASM creation routine only checks that the pointer is not zero
                        ((DialogueScriptParameter)command.Parameters[parameterIndex]).Line.Pointer = 1;
                        _script.Event.DialogueSection.Objects[command.Section.Objects[command.Index].Parameters[0]].Pointer = 1;
                    }
                    string originalText = text.GetOriginalString(_project);
                    ((DialogueScriptParameter)command.Parameters[parameterIndex]).Line.Text = originalText;
                    _script.Event.DialogueSection.Objects[command.Section.Objects[command.Index].Parameters[0]].Text = originalText;
                }
                _dialogueCancellation = null;
            }, _dialogueCancellation.Token);
            task.Start();
            _dialogueRefreshTimer.Stop();
            _dialogueRefreshTimer.Start();
            UpdateTabTitle(false, textArea);
        }
        private void DialogueRefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Instance.Invoke(() => UpdatePreview());
            _dialogueRefreshTimer.Stop();
        }
        private void DialoguePropertyDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            CharacterItem character = (CharacterItem)_project.Items.First(i => i.Type == ItemDescription.ItemType.Character && i.DisplayName.Equals(dropDown.SelectedKey));
            _log.Log($"Attempting to modify dialogue property in parameter {dropDown.ParameterIndex} to dialogue {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((DialoguePropertyScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Character = character;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)].Objects[dropDown.Command.Index]
                .Parameters[dropDown.ParameterIndex] = (short)_project.MessInfo.MessageInfos.FindIndex(m => m.Character == character.MessageInfo.Character);
            UpdateTabTitle(false, dropDown);
        }
        private void EpHeaderDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify episode header in parameter {dropDown.ParameterIndex} to speaker {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((EpisodeHeaderScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).EpisodeHeaderIndex =
                Enum.Parse<EpisodeHeaderScriptParameter.Episode>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<EpisodeHeaderScriptParameter.Episode>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void FlagTextBox_TextChanged(object sender, EventArgs e)
        {
            ScriptCommandTextBox textBox = (ScriptCommandTextBox)sender;
            if ((textBox.Text.StartsWith("F", StringComparison.OrdinalIgnoreCase) || textBox.Text.StartsWith("G", StringComparison.OrdinalIgnoreCase)) && short.TryParse(textBox.Text[1..], out short flagId))
            {
                _log.Log($"Attempting to modify parameter {textBox.ParameterIndex} to flag {textBox.Text} in {textBox.Command.Index} in file {_script.Name}...");
                bool global = textBox.Text.StartsWith("G", StringComparison.OrdinalIgnoreCase);
                ((FlagScriptParameter)textBox.Command.Parameters[textBox.ParameterIndex]).Id = global ? (short)(flagId + 100) : flagId;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(textBox.Command.Section)]
                    .Objects[textBox.Command.Index].Parameters[textBox.ParameterIndex] = ((FlagScriptParameter)textBox.Command.Parameters[textBox.ParameterIndex]).Id;
                UpdateTabTitle(false, textBox);
            }
        }
        private void FriendshipLevelDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify episode header in parameter {dropDown.ParameterIndex} to speaker {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((FriendshipLevelScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Character =
                Enum.Parse<FriendshipLevelScriptParameter.FriendshipCharacter>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<FriendshipLevelScriptParameter.FriendshipCharacter>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
        }
        private void ItemSelectionButton_SelectedChanged(object sender, EventArgs e)
        {
            CommandGraphicSelectionButton selectionButton = (CommandGraphicSelectionButton)sender;
            ItemItem selectedItem = (ItemItem)selectionButton.Selected;
            _log.Log($"Attempting to modify parameter {selectionButton.ParameterIndex} to item {((ItemItem)selectionButton.Selected).Name} in {selectionButton.Command.Index} in file {_script.Name}...");
            ((ItemScriptParameter)selectionButton.Command.Parameters[selectionButton.ParameterIndex]).ItemIndex = (short)selectedItem.ItemIndex;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selectionButton.Command.Section)].Objects[selectionButton.Command.Index].Parameters[selectionButton.ParameterIndex] = (short)selectedItem.ItemIndex;
            UpdateTabTitle(false, selectionButton);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ItemLocationDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to map {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ItemLocationScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Location = Enum.Parse<ItemItem.ItemLocation>(dropDown.SelectedKey);
            ((ItemLocationScriptParameter)_commands[dropDown.Command.Section][dropDown.Command.Index].Parameters[dropDown.ParameterIndex]).Location = Enum.Parse<ItemItem.ItemLocation>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = (short)((ItemLocationScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Location;
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ItemTransitionDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to map {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ItemTransitionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Transition = Enum.Parse<ItemItem.ItemTransition>(dropDown.SelectedKey);
            ((ItemTransitionScriptParameter)_commands[dropDown.Command.Section][dropDown.Command.Index].Parameters[dropDown.ParameterIndex]).Transition = Enum.Parse<ItemItem.ItemTransition>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = (short)((ItemTransitionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Transition;
            UpdateTabTitle(false, dropDown);
        }
        private void MapDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to map {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((MapScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Map = (MapItem)_project.FindItem(dropDown.SelectedKey);
            ((MapScriptParameter)_commands[dropDown.Command.Section][dropDown.Command.Index].Parameters[dropDown.ParameterIndex]).Map = (MapItem)_project.FindItem(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = (short)((MapScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Map.Map.Index;
            UpdateTabTitle(false, dropDown);
        }
        private void OptionDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify option in parameter {dropDown.ParameterIndex} to option {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ChoicesSectionEntry choice = _script.Event.ChoicesSection.Objects.First(c => c.Id.ToString() == dropDown.SelectedKey);
            ((OptionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Option = choice;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = (short)_script.Event.ChoicesSection.Objects.IndexOf(choice);
            UpdateTabTitle(false, dropDown);
            PopulateScriptCommands(true);
        }
        private void PaletteEffectDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to BG palette effect {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((PaletteEffectScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Effect =
                Enum.Parse<PaletteEffectScriptParameter.PaletteEffect>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<PaletteEffectScriptParameter.PaletteEffect>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void PlaceSelectionButtonSelectedChanged_Executed(object sender, EventArgs e)
        {
            CommandGraphicSelectionButton selection = (CommandGraphicSelectionButton)sender;
            _log.Log($"Attempting to modify parameter {selection.ParameterIndex} to place {((ItemDescription)selection.Selected).Name} in {selection.Command.Index} in file {_script.Name}...");
            if (((ItemDescription)selection.Selected).Name == "NONE")
            {
                ((PlaceScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Place =
                    (PlaceItem)_project.Items.FirstOrDefault(i => i.Name == ((ItemDescription)selection.Selected).Name);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] = 0;
            }
            else
            {
                ((PlaceScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Place =
                    (PlaceItem)_project.Items.FirstOrDefault(i => i.Name == ((ItemDescription)selection.Selected).Name);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                    (short)((PlaceItem)_project.Items.First(i => i.Name == ((ItemDescription)selection.Selected).Name)).Index;
            }
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ScreenSelector_ScreenChanged(object sender, EventArgs e)
        {
            ScriptCommandScreenSelector selector = (ScriptCommandScreenSelector)sender;
            _log.Log($"Attempting to modify parameter {selector.ParameterIndex} to screen {selector.SelectedScreen} in {selector.Command.Index} in file {_script.Name}...");
            ((ScreenScriptParameter)selector.Command.Parameters[selector.ParameterIndex]).Screen = selector.SelectedScreen;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selector.Command.Section)]
                .Objects[selector.Command.Index].Parameters[selector.CurrentShort] = (short)selector.SelectedScreen;
            UpdateTabTitle(false, selector);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void ScriptSectionDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to script section {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((ScriptSectionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Section = _script.Event.ScriptSections.First(s => s.Name.Replace("/", "") == dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.CurrentShort] =
                _script.Event.LabelsSection.Objects.First(l => l.Name.Replace("/", "") == dropDown.SelectedKey).Id;
            PopulateScriptCommands(true);
            UpdateTabTitle(false, dropDown);
        }
        private void SfxDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to SFX {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            SfxItem sfx = (SfxItem)_project.Items.FirstOrDefault(s => s.DisplayName == dropDown.SelectedKey);
            ((SfxScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Sfx = sfx;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = sfx.Index;

            dropDown.Link.Text = sfx.DisplayName;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.DisplayName == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false, dropDown);
        }
        private void SfxModeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to SFX mode {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SfxModeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Mode =
                Enum.Parse<SfxModeScriptParameter.SfxMode>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SfxModeScriptParameter.SfxMode>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
        }
        private void ShortNumericStepper_ValueChanged(object sender, EventArgs e)
        {
            ScriptCommandNumericStepper numericStepper = (ScriptCommandNumericStepper)sender;
            _log.Log($"Attempting to modify parameter {numericStepper.ParameterIndex} to short {numericStepper.Value} in {numericStepper.Command.Index} in file {_script.Name}...");
            ((ShortScriptParameter)numericStepper.Command.Parameters[numericStepper.ParameterIndex]).Value = (short)numericStepper.Value;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(numericStepper.Command.Section)]
                .Objects[numericStepper.Command.Index].Parameters[numericStepper.ParameterIndex] = (short)numericStepper.Value;
            if (numericStepper.SecondIndex >= 0)
            {
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(numericStepper.Command.Section)]
                    .Objects[numericStepper.Command.Index].Parameters[numericStepper.SecondIndex] = (short)numericStepper.Value;
            }
            UpdateTabTitle(false, numericStepper);
        }
        private void SpriteEntranceDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to sprite entrance {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SpriteEntranceScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).EntranceTransition =
                Enum.Parse<SpriteEntranceScriptParameter.SpriteEntranceTransition>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SpriteEntranceScriptParameter.SpriteEntranceTransition>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void SpriteExitDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to sprite exit {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SpriteExitScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ExitTransition =
                Enum.Parse<SpriteExitScriptParameter.SpriteExitTransition>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SpriteExitScriptParameter.SpriteExitTransition>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
        }
        private void SpriteShakeDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to sprite shake {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((SpriteShakeScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).ShakeEffect =
                Enum.Parse<SpriteShakeScriptParameter.SpriteShakeEffect>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<SpriteShakeScriptParameter.SpriteShakeEffect>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
        }
        private void SpriteSelectionButton_SelectionMade(object sender, EventArgs e)
        {
            CommandGraphicSelectionButton selection = (CommandGraphicSelectionButton)sender;
            _log.Log($"Attempting to modify parameter {selection.ParameterIndex} to sprite {((ItemDescription)selection.Selected).Name} in {selection.Command.Index} in file {_script.Name}...");
            if (((ItemDescription)selection.Selected).Name == "NONE")
            {
                ((SpriteScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Sprite =
                    null;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                    0;
            }
            else
            {
                ((SpriteScriptParameter)selection.Command.Parameters[selection.ParameterIndex]).Sprite =
                    (CharacterSpriteItem)selection.Selected;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(selection.Command.Section)]
                    .Objects[selection.Command.Index].Parameters[selection.ParameterIndex] =
                    (short)((CharacterSpriteItem)selection.Selected).Index;
            }
            UpdateTabTitle(false);
            Application.Instance.Invoke(() => UpdatePreview());
        }
        private void TextEntranceEffectDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to sprite shake {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((TextEntranceEffectScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).EntranceEffect =
                Enum.Parse<TextEntranceEffectScriptParameter.TextEntranceEffect>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<TextEntranceEffectScriptParameter.TextEntranceEffect>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
        }

        private void TopicDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to topic {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((TopicScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).TopicId =
                ((TopicItem)_project.Items.FirstOrDefault(i => dropDown.SelectedKey == i.DisplayName)).TopicEntry.Id;
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                ((TopicItem)_project.Items.First(i => dropDown.SelectedKey == i.DisplayName)).TopicEntry.Id;

            dropDown.Link.Text = dropDown.SelectedKey;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.DisplayName == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false, dropDown);
        }
        private void SetUpTopicControlButton_Click(object sender, EventArgs e)
        {
            TopicSelectButton button = (TopicSelectButton)sender;

            ScriptCommandDropDown topicDropDown = new() { Command = button.ScriptCommand, ParameterIndex = button.ParameterIndex };
            topicDropDown.Items.AddRange(_project.Items.Where(i => i.Type == ItemDescription.ItemType.Topic)
                .Select(t => new ListItem { Key = t.DisplayName, Text = t.DisplayName }));
            topicDropDown.SelectedIndex = 0;
            topicDropDown.SelectedIndexChanged += TopicDropDown_SelectedIndexChanged;

            StackLayout topicLink = ControlGenerator.GetFileLink(_project.Items.FirstOrDefault(i => i.Type == ItemDescription.ItemType.Topic), _tabs, _log);
            topicDropDown.Link = (ClearableLinkButton)topicLink.Items[1].Control;

            StackLayout topicLinkLayout = new()
            {
                Orientation = Orientation.Horizontal,
                Items =
                {
                    topicDropDown,
                    topicLink,
                },
            };

            button.Layout.Content = ControlGenerator.GetControlWithLabel("Topic", topicLinkLayout);
        }
        private void TransitionDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to transition {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            ((TransitionScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).Transition =
                Enum.Parse<TransitionScriptParameter.TransitionEffect>(dropDown.SelectedKey);
            _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                (short)Enum.Parse<TransitionScriptParameter.TransitionEffect>(dropDown.SelectedKey);
            UpdateTabTitle(false, dropDown);
        }
        private void VceDropDown_SelectedKeyChanged(object sender, EventArgs e)
        {
            ScriptCommandDropDown dropDown = (ScriptCommandDropDown)sender;
            _log.Log($"Attempting to modify parameter {dropDown.ParameterIndex} to voiced line {dropDown.SelectedKey} in {dropDown.Command.Index} in file {_script.Name}...");
            if (dropDown.SelectedKey == "NONE")
            {
                ((VoicedLineScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).VoiceLine = null;
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                    .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] = 0;
            }
            else
            {
                ((VoicedLineScriptParameter)dropDown.Command.Parameters[dropDown.ParameterIndex]).VoiceLine =
                    (VoicedLineItem)_project.Items.FirstOrDefault(i => i.DisplayName == dropDown.SelectedKey);
                _script.Event.ScriptSections[_script.Event.ScriptSections.IndexOf(dropDown.Command.Section)]
                    .Objects[dropDown.Command.Index].Parameters[dropDown.ParameterIndex] =
                    (short)((VoicedLineItem)_project.Items.First(i => i.DisplayName == dropDown.SelectedKey)).Index;
            }
            dropDown.Link.Text = dropDown.SelectedKey;
            dropDown.Link.RemoveAllClickEvents();
            dropDown.Link.ClickUnique += (s, e) => { _tabs.OpenTab(_project.Items.FirstOrDefault(i => i.DisplayName == dropDown.SelectedKey), _log); };

            UpdateTabTitle(false, dropDown);
        }
    }
}
