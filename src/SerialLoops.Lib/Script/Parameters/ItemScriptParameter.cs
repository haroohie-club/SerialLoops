namespace SerialLoops.Lib.Script.Parameters
{
    public class ItemScriptParameter : ScriptParameter
    {
        public short ItemIndex { get; set; }

        public ItemScriptParameter(string name, short itemIndex) : base(name, ParameterType.ITEM)
        {
            ItemIndex = itemIndex;
        }
    }
}
