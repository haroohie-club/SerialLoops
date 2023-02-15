using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Controls;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops
{
    partial class SearchDialog : FindItemsDialog
    {
        
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

            _searchInput.Focus();
        }

        private void SearchInput_OnTextChanged(object sender, EventArgs e)
        {
            string searchTerm = _searchInput.Text;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                Project.GetSearchResults(searchTerm);
            }
            else
            {
                _results.Items = Enumerable.Empty<ItemDescription>().ToList();
            }
        }

    }
}
