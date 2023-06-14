using System;
using System.Collections.Generic;
using System.Linq;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib;

public class SearchQuery
{
    public string Term { get; set; }
    public HashSet<DataHolder> Scopes { get; set; } = new() { DataHolder.Title };
    public HashSet<ItemDescription.ItemType> Types { get; set; } = Enum.GetValues<ItemDescription.ItemType>().ToHashSet();
    public bool QuickSearch => !Scopes.Any(scope => (int)scope > 10);

    public enum DataHolder
    {
        // Quick search filters
        Title = 1,

        // Deep search filters
        Dialogue_Text = 11,
        Script_Flag = 12,
        Speaker_Name = 13,
        Conditional = 14,
        Background_Type = 15
    }

    public static SearchQuery Create(string text)
    {
        return new() { Term = text };
    }
}