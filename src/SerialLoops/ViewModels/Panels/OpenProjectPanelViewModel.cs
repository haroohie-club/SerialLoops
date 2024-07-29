namespace SerialLoops.ViewModels.Panels
{
    public class OpenProjectPanelViewModel : ViewModelBase
    {
        public ItemExplorerPanelViewModel Explorer { get; set; }
        public EditorTabsPanelViewModel EditorTabs { get; set; }

        public OpenProjectPanelViewModel(ItemExplorerPanelViewModel explorer, EditorTabsPanelViewModel editorTabs)
        {
            Explorer = explorer;
            EditorTabs = editorTabs;
        }
    }
}
