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
    public bool QuickSearch => !Scopes.Any(scope => (int)scope >= 10);

    public enum DataHolder
    {
        // Quick search filters
        Title = 1,
        Background_ID = 2,
        Archive_Index = 3,

        // Deep search filters
        Archive_Filename = 10,
        Dialogue_Text = 11,
        Command = 12,
        Flag = 13,
        Speaker_Name = 14,
        Conditional = 15,
        Background_Type = 16,
        Episode_Number = 17,
        Episode_Unique = 18,
        Orphaned_Items = 19
    }

    public static SearchQuery Create(string text)
    {
        return new() { Term = text };
    }
}
