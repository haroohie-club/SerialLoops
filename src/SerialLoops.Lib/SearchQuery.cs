using System;
using System.Collections.Generic;
using System.Linq;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib;

public class SearchQuery {

    public string Text;
    public Dictionary<Filter, string> Filters = new();
    public HashSet<Flag> Flags = new();
    public HashSet<ItemDescription.ItemType> Types = Enum.GetValues<ItemDescription.ItemType>().ToHashSet();

    public enum Filter {
        Title,
        Dialogue_Text,
        Speaker_Name,
        Conditional,
        Background_Type
    }

    public enum Flag {
        Only_Titles
    }

    public static SearchQuery Create(string text)
    {
        return new() {Text = text};
    }

    public static Filter? GetFilter(string text)
    {
        Enum.TryParse(text, true, out Filter filter);
        return filter;
    }

    public static Flag? GetFlag(string text)
    {
        Enum.TryParse(text, true, out Flag flag);
        return flag;
    }

    public bool IsFlagSet(Flag flag)
    {
        return Flags.Contains(flag);
    }

}