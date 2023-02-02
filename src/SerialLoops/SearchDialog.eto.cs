using Eto.Drawing;
using Eto.Forms;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops
{
    partial class SearchDialog : Dialog
    {
        public ILogger Log;
        public Project OpenProject;
        public EditorTabsPanel EditorTabs;

        private ItemResultsPanel _results;
        private TextBox _searchInput;

        void InitializeComponent()
        {
            Title = "Find in Project";
            MinimumSize = new Size(300, 200);
            Padding = 10;

            _results = new(new List<ItemDescription>(), Log)
            {
                Dialog = this
            };
            _searchInput = new()
            {
                PlaceholderText = "Search...",
                Size = new Size(200, 25)
            };
            _searchInput.TextChanged += SearchInput_OnTextChanged;

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Spacing = 10,
                Padding = 10,
                Items =
                {
                    new GroupBox
                    {
                        Text = "Search",
                        Padding = 10,
                        Content = new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 10,
                            Items =
                            {
                                "Find: ",
                                _searchInput
                            }
                        }
                    },
                    _results
                }
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            if (Log is null)
            {
                // We can't log that log is null, so we have to throw
                throw new LoggerNullException();
            }
            if (OpenProject is null)
            {
                Log.LogError($"Project not provided to project creation dialog");
                Close();
            }
            if (EditorTabs is null)
            {
                Log.LogError($"Editor Tabs not provided to project creation dialog");
                Close();
            }
            base.OnLoad(e);
        }

        private void SearchInput_OnTextChanged(object sender, EventArgs e)
        {
            string searchTerm = _searchInput.Text;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                _results.Items = OpenProject.Items.Where(item => item.Name.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            } 
            else
            {
                _results.Items = Enumerable.Empty<ItemDescription>().ToList();
            }
        }

    }
}
