using Avalonia;
using Avalonia.Controls;

namespace SerialLoops.Views.Panels
{
    public partial class ItemExplorerPanel : Panel
    {
        public static readonly AvaloniaProperty<bool?> ExpandItemsProperty = AvaloniaProperty.Register<ItemExplorerPanel, bool?>(nameof(ExpandItems));

        public bool? ExpandItems
        {
            get => this.GetValue<bool?>(ExpandItemsProperty);
            set => SetValue(ExpandItemsProperty, value);
        }

        public ItemExplorerPanel()
        {
            InitializeComponent();
        }
    }
}
