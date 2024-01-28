using System;
using System.Collections.Generic;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Editors;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System.Linq;

namespace SerialLoops.Controls
{
    public class EditorTabsPanel : Panel
    {
        public DocumentControl Tabs { get; private set; }

        private readonly Project _project;
        private readonly ILogger _log;
        private readonly MainForm _mainForm;
        private ToolBar _toolBar => _mainForm.ToolBar;
        private MenuBar _menuBar => _mainForm.Menu;

        public EditorTabsPanel(Project project, MainForm mainForm, ILogger log)
        {
            _project = project;
            _log = log;
            _mainForm = mainForm;
            InitializeComponent();
        }

        void InitializeComponent()
        {
            Padding = 0;

            Tabs = new()
            {
                AllowReordering = true,
                Enabled = true
            };
            Tabs.PageClosed += Tabs_PageClosed;
            Tabs.SelectedIndexChanged += Tabs_PageChanged;
            Content = new TableLayout(Tabs);
            ContextMenu = new TabContextMenu(this, _log);
        }

        internal void OpenTab(ItemDescription item, ILogger log)
        {
            // If a tab page with the name and type exists, switch to it
            foreach (DocumentPage page in Tabs.Pages)
            {
                if (page.Text == item.DisplayNameWithStatus)
                {
                    Tabs.SelectedPage = page;
                    return;
                }
            }

            // Open a new editor for the item -- This is where the item can be loaded from the project files
            DocumentPage newPage = CreateTab(item, _project, log);
            if (Platform.IsMac)
            {
                newPage.ContextMenu = new();
            }
            else
            {
                newPage.ContextMenu = null;
            }

            Tabs.Pages.Add(newPage);
            Tabs.SelectedPage = newPage;
            newPage.Closed += Tabs_PageChanged;
            Tabs_PageChanged(this, EventArgs.Empty);
        }

        private DocumentPage CreateTab(ItemDescription item, Project project, ILogger log)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditor((BackgroundItem)project.Items.First(i => i.Name == item.Name),
                        project, log);
                case ItemDescription.ItemType.BGM:
                    return new BackgroundMusicEditor(
                        (BackgroundMusicItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Character:
                    return new CharacterEditor((CharacterItem)project.Items.First(i => i.Name == item.Name), project, this, log);
                case ItemDescription.ItemType.Character_Sprite:
                    return new CharacterSpriteEditor(
                        (CharacterSpriteItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Chibi:
                    return new ChibiEditor((ChibiItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Group_Selection:
                    return new GroupSelectionEditor((GroupSelectionItem)project.Items.First(i => i.Name == item.Name), log, project, this);
                case ItemDescription.ItemType.Item:
                    return new ItemEditor((ItemItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Map:
                    return new MapEditor((MapItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Place:
                    return new PlaceEditor((PlaceItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Puzzle:
                    return new PuzzleEditor((PuzzleItem)project.Items.First(i => i.Name == item.Name), project, this, log);
                case ItemDescription.ItemType.Scenario:
                    return new ScenarioEditor((ScenarioItem)project.Items.First(i => i.Name == item.Name), log, project, this);
                case ItemDescription.ItemType.Script:
                    return new ScriptEditor((ScriptItem)project.Items.First(i => i.Name == item.Name), log, project, this);
                case ItemDescription.ItemType.SFX:
                    return new SfxEditor((SfxItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.System_Texture:
                    return new SystemTextureEditor((SystemTextureItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Topic:
                    return new TopicEditor((TopicItem)project.Items.First(i => i.Name == item.Name), project, this, log);
                case ItemDescription.ItemType.Voice:
                    return new VoicedLineEditor((VoicedLineItem)project.Items.First(i => i.Name == item.Name), project, log);
                default:
                    log.LogError("Invalid item type!");
                    return null;
            }
        }

        private void Tabs_PageClosed(object sender, DocumentPageEventArgs e)
        {
            if (e.Page.GetType() == typeof(BackgroundMusicEditor))
            {
                ((BackgroundMusicEditor)e.Page).BgmPlayer.Stop();
            }
            else if (e.Page.GetType() == typeof(VoicedLineEditor))
            {
                ((VoicedLineEditor)e.Page).VcePlayer.Stop();
            }
            else if (e.Page.GetType() == typeof(SfxEditor))
            {
                ((SfxEditor)e.Page).Player.Dispose();
            }

            if (Tabs.SelectedPage is null)
            {
                ClearEditorCommands();
            }
        }

        public void Tabs_PageChanged(object sender, EventArgs e)
        {
            ClearEditorCommands();

            // Add editor-specific toolbar commands
            List<Command> commands = ((Editor)Tabs.SelectedPage)?.EditorCommands;
            if (commands is null || commands.Count == 0) return;

            SubMenuItem editItem = new() { Text = "&Edit", Tag = Editor.EDITOR_TOOLBAR_TAG };
            SeparatorToolItem separator = new() { Tag = Editor.EDITOR_TOOLBAR_TAG, Style = "sl-toolbar-separator" };
            _toolBar?.Items.Insert(0, separator);
            commands.ForEach(command =>
            {
                ButtonToolItem toolButton = new(command) { Tag = Editor.EDITOR_TOOLBAR_TAG, Style = "sl-toolbar-button" };
                _toolBar?.Items.Insert(0, toolButton);
                editItem?.Items.Insert(0, command);
            });
            _menuBar?.Items.Add(editItem);
        }

        private void ClearEditorCommands()
        {
            _toolBar?.Items
                .Where(toolItem => toolItem.Tag != null && toolItem.Tag.Equals(Editor.EDITOR_TOOLBAR_TAG)).ToList()
                .ForEach(toolItem => _toolBar.Items.Remove(toolItem));
            _menuBar?.Items
                .Where(menuItem => menuItem.Tag != null && menuItem.Tag.Equals(Editor.EDITOR_TOOLBAR_TAG)).ToList()
                .ForEach(menuItem => _menuBar.Items.Remove(menuItem));
        }
    }
}