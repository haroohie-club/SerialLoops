using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using System;

namespace SerialLoops.Controls
{
    internal class TabContextMenu : ContextMenu
    {

        private readonly ILogger _log;
        private readonly EditorTabsPanel _tabs;

        public TabContextMenu(EditorTabsPanel tabs, ILogger log) : base()
        {
            _tabs = tabs;
            _log = log;

            Opening += ContextMenu_OnOpen;

            Command closeTabCommand = new();
            closeTabCommand.Executed += (sender, args) =>
            {
                _tabs.Tabs_PageClosed(sender, new(_tabs.Tabs.SelectedPage));
                _tabs.Tabs.Remove(_tabs.Tabs.SelectedPage);
            };
            Items.Add(new ButtonMenuItem
            {
                Text = "Close",
                Command = closeTabCommand
            });

            Command closeTabsToRightCommand = new();
            closeTabsToRightCommand.Executed += (sender, args) =>
            {
                int index = _tabs.Tabs.SelectedIndex;
                for (int i = _tabs.Tabs.Pages.Count - 1; i > index; i--)
                {
                    _tabs.Tabs.Remove(_tabs.Tabs.Pages[i]);
                }
            };
            Items.Add(new ButtonMenuItem
            {
                Text = "Close All To Right",
                Command = closeTabsToRightCommand
            });

            Command closeAllTabsCommand = new();
            closeAllTabsCommand.Executed += (sender, args) =>
            {
                foreach (var tab in _tabs.Tabs.Pages)
                {
                    _tabs.Tabs_PageClosed(sender, new(tab));
                }
                _tabs.Tabs.Pages.Clear();
            };
            Items.Add(new ButtonMenuItem
            {
                Text = "Close All",
                Command = closeAllTabsCommand
            });

            Command closeAllTabsButThisCommand = new();
            closeAllTabsButThisCommand.Executed += (sender, args) =>
            {
                DocumentPage @this = _tabs.Tabs.SelectedPage;
                _tabs.Tabs.Pages.Clear();
                _tabs.Tabs.Pages.Add(@this);
            };
            Items.Add(new ButtonMenuItem
            {
                Text = "Close All But This",
                Command = closeAllTabsButThisCommand
            });
        }

        // When the context menu is opened
        private void ContextMenu_OnOpen(object sender, EventArgs args)
        {
            // If there is only one tab, disable the "Close All" and "Close All But This" options
            if (_tabs.Tabs.Pages.Count == 1)
            {
                Items[2].Enabled = false;
                Items[3].Enabled = false;
            }
            else
            {
                Items[2].Enabled = true;
                Items[3].Enabled = true;
            }

            // If the clicked tab is the last tab, disable the "Close All To Right" option
            if (_tabs.Tabs.Pages.IndexOf(_tabs.Tabs.SelectedPage) == _tabs.Tabs.Pages.Count - 1)
            {
                Items[1].Enabled = false;
            }
            else
            {
                Items[1].Enabled = true;
            }
        }

    }
}
