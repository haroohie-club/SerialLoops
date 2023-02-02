namespace SerialLoops.Lib.Items
{
    public class ItemDescription
    {

        public string Name { get; protected set; }
        public ItemType Type { get; private set; }

        public ItemDescription(string name, ItemType type)
        {
            Name = name;
            Type = type;
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
            System_Texture,
            Topic,
            Transition,
            Tutorial,
            Voice,
        }

    }
}
