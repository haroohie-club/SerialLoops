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

        public EditorTabsPanel(Project project, ILogger log)
        {
            _project = project;
            _log = log;
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
        }

        internal DocumentPage CreateTab(ItemDescription item, Project project, ILogger log)
        {
            switch (item.Type)
            {
                case ItemDescription.ItemType.Background:
                    return new BackgroundEditor((BackgroundItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.BGM:
                    return new BackgroundMusicEditor((BackgroundMusicItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Character_Sprite:
                    return new CharacterSpriteEditor((CharacterSpriteItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Chibi:
                    return new ChibiEditor((ChibiItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Dialogue_Config:
                    return new DialogueConfigEditor((DialogueConfigItem)project.Items.First(i => i.Name == item.Name), log);
                case ItemDescription.ItemType.Group_Selection:
                    return new GroupSelectionEditor((GroupSelectionItem)project.Items.First(i => i.Name == item.Name), log, project, this);
                case ItemDescription.ItemType.Map:
                    return new MapEditor((MapItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Puzzle:
                    return new PuzzleEditor((PuzzleItem)project.Items.First(i => i.Name == item.Name), project, this, log);
                case ItemDescription.ItemType.Scenario:
                    return new ScenarioEditor((ScenarioItem)project.Items.First(i => i.Name == item.Name), log, project, this);
                case ItemDescription.ItemType.Script:
                    return new ScriptEditor((ScriptItem)project.Items.First(i => i.Name == item.Name), project, log, this);
                case ItemDescription.ItemType.Topic:
                    return new TopicEditor((TopicItem)project.Items.First(i => i.Name == item.Name), project, log);
                case ItemDescription.ItemType.Voice:
                    return new VoicedLineEditor((VoicedLineItem)project.Items.First(i => i.Name == item.Name), log);
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
        }
    }
}
