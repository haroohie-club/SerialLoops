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
    partial class SearchDialog : FindItemsWindow
    {
        private ItemResultsPanel _results;
        private SearchBox _searchInput;
        
        public string Text { get => _searchInput.Text; set => _searchInput.Text = value; }
        private HashSet<SearchQuery.DataHolder> _scopes = new() { SearchQuery.DataHolder.Title };
        private HashSet<ItemDescription.ItemType> _types = Enum.GetValues<ItemDescription.ItemType>().ToHashSet();
        private Label _searchWarningLabel = new()
        {
            Text = "Press ENTER to execute search.",
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
        };
        private Label _resultsLabel = new()
        {
            Text = "Results",
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
        };
        
        private SearchQuery _query
        {
            get => new()
            {
                Term = Text,
                Scopes = _scopes,
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
                Window = this
            };
            _searchInput = new()
            {
                PlaceholderText = "Search...",
                Size = new Size(250, 25)
            };
            _searchInput.TextChanged += SearchInput_OnTextChanged;
            _searchInput.KeyDown += SearchInput_OnKeyDown;

            Content = new TableLayout(_searchInput, GetFiltersPanel(), _searchWarningLabel, _resultsLabel, _results)
            {
                Spacing = new(5, 5)
            };
            _searchInput.Focus();
        }

        private void Search(bool force = false)
        {
            var query = _query;
            _searchWarningLabel.Visible = !query.QuickSearch;
            _resultsLabel.Visible = false;
            if (!query.QuickSearch && !force)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(query.Term))
            {
                return;
            }

            if (query.QuickSearch)
            {
                var results = Project.GetSearchResults(query, Log);
                _results.Items = results;
                _resultsLabel.Text = $"{results.Count} results found";
                _resultsLabel.Visible = true;
            }
            else
            {
                if (query.Scopes.Count is 0 || query.Types.Count is 0)
                {
                    MessageBox.Show("Please select at least one search scope and item filter.", "Invalid search terms", MessageBoxType.Error);
                    return;
                }
                LoopyProgressTracker tracker = new("Searching");
                List<ItemDescription> results = new();
                _ = new ProgressDialog(() => results = Project.GetSearchResults(query, Log, tracker),
                    () =>
                    {
                        _results.Items = results;
                        _resultsLabel.Text = $"{results.Count} results found";
                        _resultsLabel.Visible = true;
                    }, tracker, $"Searching {Project.Name}...");
            }
        }

        private Container GetFiltersPanel()
        {
            List<Option> searchScopes = new();
            foreach (SearchQuery.DataHolder scope in Enum.GetValues(typeof(SearchQuery.DataHolder)))
            {
                searchScopes.Add(new BooleanOption
                {
                    Name = scope.ToString().Replace("_", " "),
                    OnChange = value =>
                    {
                        if (value)
                        {
                            _scopes.Add(scope);
                        }
                        else
                        {
                            _scopes.Remove(scope);
                        }
                        Search();
                    },
                    Value = _scopes.Contains(scope)
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
            
            // Add a toggle for item filters
            typeOptions.Add(new BooleanToggleOption(typeOptions) { Name = "Quick Toggle" });
            
            // Ensure the number of items is divisible by 3, add blank columns if not
            while (typeOptions.Count % 3 != 0)
            {
                typeOptions.Add(new BlankOption());
            }

            return new TableLayout(
                new TableRow(
                    new OptionsGroup("Search Scope", searchScopes),
                    new OptionsGroup("Filter by Item", typeOptions, 3)
                )
            );
        }

        private void SearchInput_OnTextChanged(object sender, EventArgs e)
        {
            Search();
        }

        private void SearchInput_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Keys.Enter)
            {
                Search(true);
            }
        }
        
    }

    internal class BlankOption : Option {
        protected override Control GetControl()
        {
            return new Panel();
        }
    }
    
}
