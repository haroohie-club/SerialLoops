using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialLoops
{
    partial class SearchDialog : FindItemsDialog
    {
        private ItemResultsPanel _results;
        private SearchBox _searchInput;
        private bool _titlesOnly = true;
        public string Text { get => _searchInput.Text; set => _searchInput.Text = value; }

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
            _searchInput.TextChanging += SearchInput_OnTextChanging;
            
            CheckBox titlesOnlyBox = new() { Checked = _titlesOnly };
            titlesOnlyBox.CheckedChanged += TitlesOnlyBox_CheckedChanged;

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
                                _searchInput,
                                ControlGenerator.GetControlWithLabel("Titles Only?", titlesOnlyBox),
                            }
                        }
                    },
                    _results
                }
            };

            _searchInput.Focus();
        }
        
        private void Search(string searchTerm)
        {
            _results.Items = !string.IsNullOrWhiteSpace(searchTerm) ? Project.GetSearchResults(searchTerm, _titlesOnly) 
                : Enumerable.Empty<ItemDescription>().ToList();
        }

        private void SearchInput_OnTextChanging(object sender, TextChangingEventArgs e)
        {
            Search(e.NewText);
        }

        private void TitlesOnlyBox_CheckedChanged(object sender, EventArgs e)
        {
            _titlesOnly = ((CheckBox)sender).Checked ?? false;
            Search(_searchInput.Text);
        }
    }
}
