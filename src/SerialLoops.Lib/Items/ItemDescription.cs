namespace SerialLoops.Lib.Items
{
    public class ItemDescription
    {

        public string Name { get; protected set; }
        public string DisplayName { get; protected set; }
        public string DisplayNameWithStatus => UnsavedChanges ? $"{DisplayName} *" : DisplayName;
        public ItemType Type { get; private set; }
        public bool UnsavedChanges { get; set; } = false;

        public ItemDescription(string name, ItemType type, string displayName)
        {
            Name = name;
            Type = type;
            if (!string.IsNullOrEmpty(displayName))
            {
                DisplayName = displayName;
            }
            else
            {
                DisplayName = Name;
            }
        }

        // Enum with values for each type of item
        public enum ItemType
        {
            Background,
            BGM,
            Character_Sprite,
            Chess,
            Chibi,
            Dialogue_Config,
            Group_Selection,
            Map,
            Puzzle,
            Scenario,
            Script,
            Sfx,
            System_Texture,
            Topic,
            Transition,
            Tutorial,
            Voice,
        }

    }
}
