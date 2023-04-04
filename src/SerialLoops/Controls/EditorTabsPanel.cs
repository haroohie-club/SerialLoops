﻿using System;
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
        public ToolBar ToolBar { get; }
        
        private readonly Project _project;
        private readonly ILogger _log;

        public EditorTabsPanel(Project project, ToolBar toolBar, ILogger log)
        {
            _project = project;
            _log = log;
            ToolBar = toolBar;
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
            Tabs.PageClosed += Tabs_PageClosed;
            Tabs.SelectedIndexChanged += Tabs_PageOpened;
        }

        private DocumentPage CreateTab(ItemDescription item, Project project, ILogger log)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditor((BackgroundItem)project.Items.First(i => i.Name == item.Name), this, log);
                case ItemDescription.ItemType.BGM:
                    return new BackgroundMusicEditor((BackgroundMusicItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Character_Sprite:
                    return new CharacterSpriteEditor((CharacterSpriteItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Chibi:
                    return new ChibiEditor((ChibiItem)project.Items.First(i => i.Name == item.Name), this, log);
                case ItemDescription.ItemType.Dialogue_Config:
                    return new DialogueConfigEditor((DialogueConfigItem)project.Items.First(i => i.Name == item.Name), this, log);
                case ItemDescription.ItemType.Group_Selection:
                    return new GroupSelectionEditor((GroupSelectionItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Map:
                    return new MapEditor((MapItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Place:
                    return new PlaceEditor((PlaceItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Puzzle:
                    return new PuzzleEditor((PuzzleItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Scenario:
                    return new ScenarioEditor((ScenarioItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Script:
                    return new ScriptEditor((ScriptItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Topic:
                    return new TopicEditor((TopicItem)project.Items.First(i => i.Name == item.Name), this, project, log);
                case ItemDescription.ItemType.Voice:
                    return new VoicedLineEditor((VoicedLineItem)project.Items.First(i => i.Name == item.Name), this, project, log);
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
            ToolBar?.Items
                .Where(toolItem => toolItem.Tag != null && toolItem.Tag.Equals(Editor.EDITOR_TOOLBAR_TAG)).ToList()
                .ForEach(toolItem => ToolBar.Items.Remove(toolItem));
        }

        private void Tabs_PageOpened(object sender, EventArgs e)
        {
            // Add editor-specific toolbar commands
            ToolBar?.Items
                .Where(toolItem => toolItem.Tag != null && toolItem.Tag.Equals(Editor.EDITOR_TOOLBAR_TAG)).ToList()
                .ForEach(toolItem => ToolBar.Items.Remove(toolItem));

            List<Command> commands = ((Editor) Tabs.SelectedPage)?.ToolBarCommands;
            if (commands is {Count: <= 0}) return;
            
            SeparatorToolItem separator = new() { Tag = Editor.EDITOR_TOOLBAR_TAG };
            ToolBar?.Items.Insert(0, separator);
            commands?.ForEach(command =>
            {
                ButtonToolItem toolButton = new(command) { Tag = Editor.EDITOR_TOOLBAR_TAG };
                ToolBar?.Items.Insert(0, toolButton);
            });
        }
    }
}
