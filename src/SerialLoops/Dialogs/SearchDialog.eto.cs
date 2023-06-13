using Eto.Drawing;
using Eto.Forms;
using SerialLoops.Controls;
using SerialLoops.Dialogs;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using SerialLoops.Utility;

namespace SerialLoops
{
    partial class SearchDialog : FindItemsDialog
    {
        private ItemResultsPanel _results;
        private SearchBox _searchInput;
        
        public string Text { get => _searchInput.Text; set => _searchInput.Text = value; }
        private Dictionary<SearchQuery.Filter, string> _filters = new();
        private HashSet<SearchQuery.Flag> _flags = new();
        private HashSet<ItemDescription.ItemType> _types = Enum.GetValues<ItemDescription.ItemType>().ToHashSet();
        private SearchQuery _query
        {
            get => new()
            {
                Text = Text,
                Filters = _filters,
                Flags = _flags,
                Types = _types
            };
        }

        void InitializeComponent()
        {
            Title = "Find in Project";
            MinimumSize = new Size(500, 600);
            Padding = 10;

            _results = new(new List<ItemDescription>(), Log)
            {
                Dialog = this
            };
            _searchInput = new()
            {
                PlaceholderText = "Search...",
                Size = new Size(250, 25)
            };
            _searchInput.TextChanged += SearchInput_OnTextChanged;

            Content = new TableLayout(_searchInput, GetFiltersPanel(), _results) { Spacing = new(10, 10) };
            _searchInput.Focus();
        }
        
        private void Search()
        {
            _results.Items = !string.IsNullOrWhiteSpace(_query.Text)
                ? Project.GetSearchResults(_query) 
                : Enumerable.Empty<ItemDescription>().ToList();
        }

        private Container GetFiltersPanel()
        {
            
            
            List<Option> flagOptions = new();
            foreach (SearchQuery.Flag flag in Enum.GetValues(typeof(SearchQuery.Flag)))
            {
                flagOptions.Add(new BooleanOption
                {
                    Name = flag.ToString().Replace("_", " "),
                    OnChange = value =>
                    {
                        if (value)
                        {
                            _flags.Add(flag);
                        }
                        else
                        {
                            _flags.Remove(flag);
                        }
                        Search();
                    },
                    Value = _flags.Contains(flag)
                });
            }
            
            List<Option> typeOptions = new();
            foreach (ItemDescription.ItemType type in Enum.GetValues(typeof(ItemDescription.ItemType)))
            {
                typeOptions.Add(new ItemBooleanOption(Log)
                {
                    Name = type.ToString(),
                    Type = type,
                    OnChange = value =>
                    {
                        if (value)
                        {
                            _types.Add(type);
                        }
                        else
                        {
                            _types.Remove(type);
                        }
                        Search();
                    },
                    Value = _types.Contains(type)
                });
            }
            
            return new TableLayout(new TableRow(
                new OptionsGroup("Search Options", flagOptions),
                new OptionsGroup("Item Filter", typeOptions, 3)
            ));
        }

        private void SearchInput_OnTextChanged(object sender, EventArgs e)
        {
            Search();
        }
        
    }
}
