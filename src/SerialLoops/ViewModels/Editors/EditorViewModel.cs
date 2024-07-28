using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;

namespace SerialLoops.ViewModels.Editors
{
    public class EditorViewModel : ViewModelBase
    {
        protected ILogger _log;
        protected Project _project;

        public ItemDescription Description { get; }
    }
}
