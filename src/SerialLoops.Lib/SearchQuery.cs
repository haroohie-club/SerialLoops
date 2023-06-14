using System;
using System.Collections.Generic;
using System.Linq;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib;

public class SearchQuery {

    public string Term { get; set; }
    public HashSet<DataHolder> Scopes { get; set; } = new() { DataHolder.Title };
    public HashSet<ItemDescription.ItemType> Types { get; set; } = Enum.GetValues<ItemDescription.ItemType>().ToHashSet();
    public bool QuickSearch => !Scopes.Any(scope => (int) scope > 2);

    public enum DataHolder {
        // Quick search filters
        Title = 1,
        Cached_Text = 2,
        
        // Deep search filters
        Dialogue_Text = 3,
        Script_Flag = 4,
        Speaker_Name = 5,
        Conditional = 6,
        Background_Type = 7
    }

    public static SearchQuery Create(string text)
    {
        return new() { Term = text };
    }

}