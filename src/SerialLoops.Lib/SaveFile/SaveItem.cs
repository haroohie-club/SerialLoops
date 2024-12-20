using System.IO;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Items;
using GameSaveFile = HaruhiChokuretsuLib.Save.SaveFile;

namespace SerialLoops.Lib.SaveFile;

public class SaveItem(string path, string displayName) : Item(Path.GetFileNameWithoutExtension(path), ItemType.Save, displayName)
{
    public GameSaveFile Save { get; set; } = new(File.ReadAllBytes(path));
    public string SaveLoc { get; set; } = path;

    public override void Refresh(Project project, ILogger log)
    {
    }
}
