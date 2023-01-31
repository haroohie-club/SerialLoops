using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Items
{
    public abstract class Item : ItemDescription
    {

        public Item(string name, ItemDescription.ItemType type) : base(name, type)
        {
        }

    }

}