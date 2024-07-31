using System;
using System.Collections.Generic;
using System.Linq;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib;

public class SearchQuery
{
    public string Term { get; set; }
    public HashSet<DataHolder> Scopes { get; set; } = [DataHolder.Title];
    public HashSet<ItemDescription.ItemType> Types { get; set; } = [.. Enum.GetValues<ItemDescription.ItemType>()];
    public bool QuickSearch => !Scopes.Any(scope => (int)scope > 10);

    public enum DataHolder
    {
        // Quick search filters
        Title = 1,
        Background_ID = 2,

        // Deep search filters
        Dialogue_Text = 11,
        Flag = 12,
        Speaker_Name = 13,
        Conditional = 14,
        Background_Type = 15,
        Episode_Number = 16,
        Episode_Unique = 17,
    }

    public static SearchQuery Create(string text)
    {
        return new() { Term = text };
    }
}
