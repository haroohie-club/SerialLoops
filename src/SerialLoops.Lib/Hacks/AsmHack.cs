using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Lib.Hacks;

public class AsmHack : ReactiveObject
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<InjectionSite> InjectionSites { get; set; }
    public List<HackFile> Files { get; set; }
    [JsonIgnore]
    public bool ValueChanged { get; set; }
    [JsonIgnore]
    [Reactive]
    public bool IsApplied { get; set; }

    public bool Applied(Project project)
    {
        foreach (InjectionSite site in InjectionSites)
        {
            if (site.Code.Equals("ARM9"))
            {
                using FileStream arm9 = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "arm9.bin"));
                if (site.Repl)
                {
                    using FileStream origArm9 = File.OpenRead(Path.Combine(project.BaseDirectory, "src", "arm9.bin"));
                    origArm9.Seek(site.Offset, SeekOrigin.Begin);
                    arm9.Seek(site.Offset, SeekOrigin.Begin);
                    byte[] origBytes = new byte[4];
                    byte[] newBytes = new byte[4];
                    origArm9.ReadExactly(origBytes, 0, 4);
                    arm9.ReadExactly(newBytes, 0, 4);
                    return HaruhiChokuretsuLib.Util.IO.ReadInt(origBytes, 0) !=
                           HaruhiChokuretsuLib.Util.IO.ReadInt(newBytes, 0);
                }

                // All BL functions start with 0xEB
                arm9.Seek(site.Offset + 3, SeekOrigin.Begin);
                return arm9.ReadByte() == 0xEB;
            }
            else
            {
                using FileStream overlay = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "overlay", $"main_{int.Parse(site.Code):X4}.bin"));
                if (site.Repl)
                {
                    using FileStream origOvl = File.OpenRead(Path.Combine(project.BaseDirectory, "src", "arm9.bin"));
                    origOvl.Seek(site.Offset, SeekOrigin.Begin);
                    overlay.Seek(site.Offset, SeekOrigin.Begin);
                    byte[] origBytes = new byte[4];
                    byte[] newBytes = new byte[4];
                    origOvl.ReadExactly(origBytes, 0, 4);
                    overlay.ReadExactly(newBytes, 0, 4);
                    return HaruhiChokuretsuLib.Util.IO.ReadInt(origBytes, 0) !=
                           HaruhiChokuretsuLib.Util.IO.ReadInt(newBytes, 0);
                }

                overlay.Seek(site.Offset + 3, SeekOrigin.Begin);
                return overlay.ReadByte() == 0xEB;
            }
        }
        return false;
    }

    public void Apply(Project project, ConfigUser configUser, Dictionary<HackFile, SelectedHackParameter[]> selectedParameters, ILogger log, bool forceApplication = false)
    {
        if (Applied(project) && !forceApplication)
        {
            log.LogWarning($"Hack '{Name}' already applied, skipping.");
            return;
        }

        foreach (HackFile file in Files)
        {
            string destination = Path.Combine(project.BaseDirectory, "src", file.Destination);
            if (!Directory.Exists(Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
            }
            string fileText = File.ReadAllText(Path.Combine(configUser.HacksDirectory, file.File));
            foreach (SelectedHackParameter parameter in selectedParameters[file])
            {
                fileText = fileText.Replace($"{{{{{parameter.Parameter.Name}}}}}", parameter.Value);
            }
            File.WriteAllText(destination, fileText);
        }
    }

    public void Revert(Project project, ILogger log)
    {
        bool oneSuccess = false;
        foreach (HackFile file in Files)
        {
            string fileToDelete = Path.Combine(project.BaseDirectory, "src", file.Destination);
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
                oneSuccess = true;
                if (Path.GetFileNameWithoutExtension(fileToDelete) ==
                    Path.GetFileName(Path.GetDirectoryName(fileToDelete)))
                {
                    Directory.Delete(Path.GetDirectoryName(fileToDelete)!);
                }
            }
        }
        // If there's at least one success, we assume that an older version of the hack was applied and we've now rolled it back
        if (!oneSuccess)
        {
            log.LogError(string.Format(project.Localize("ErrorFailedDeletingHackFiles"), Name));
            IsApplied = true;
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is string name)
        {
            return name.Equals(Name);
        }
        return ((AsmHack)obj).Name.Equals(Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public bool DeepEquals(AsmHack other)
    {
        return other.Name.Equals(Name) && other.Description.Equals(Description) && other.InjectionSites.SequenceEqual(InjectionSites) && other.Files.SequenceEqual(Files);
    }
}

public class InjectionSite
{
    [JsonIgnore]
    public uint Offset { get; set; }
    public bool Repl { get; set; }
    public string Code { get; set; }
    public string Location
    {
        get
        {
            int startAddress;
            if (!Code.Equals("ARM9"))
            {
                startAddress = 0x020C7660;
            }
            else
            {
                startAddress = 0x02000000;
            }
            return $"{(uint)(Offset + startAddress):X8}";
        }
        set
        {
            int startAddress;
            if (!Code.Equals("ARM9"))
            {
                startAddress = 0x020C7660;
            }
            else
            {
                startAddress = 0x02000000;
            }
            Offset = (uint)(uint.Parse(value, System.Globalization.NumberStyles.HexNumber) - startAddress);
        }
    }

    public override bool Equals(object obj)
    {
        return ((InjectionSite)obj).Offset == Offset && ((InjectionSite)obj).Code.Equals(Code);
    }

    public override int GetHashCode()
    {
        return Offset.GetHashCode() * Code.GetHashCode() - (Offset.GetHashCode() + Code.GetHashCode());
    }
}

public class HackFile
{
    public string File { get; set; }
    public string Destination { get; set; }
    public string[] Symbols { get; set; }
    public HackParameter[] Parameters { get; set; }

    public override bool Equals(object obj)
    {
        return (((HackFile)obj).File?.Equals(File) ?? false) && (((HackFile)obj).Destination?.Equals(Destination) ?? false) && (((HackFile)obj)?.Symbols.SequenceEqual(Symbols) ?? false) && (((HackFile)obj).Parameters?.SequenceEqual(Parameters) ?? false);
    }

    public override int GetHashCode()
    {
        return File.GetHashCode();
    }
}

public class SelectedHackParameter
{
    public HackParameter Parameter { get; set; }
    public int Selection { get; set; } = -1;
    public string Value => Parameter.Values[Selection].Value;
}

public class HackParameter
{
    public string Name { get; set; }
    public string DescriptiveName { get; set; }
    public HackParameterValue[] Values { get; set; }

    public override bool Equals(object obj)
    {
        return (((HackParameter)obj).Name?.Equals(Name) ?? false) && (((HackParameter)obj).DescriptiveName?.Equals(DescriptiveName) ?? false) && (((HackParameter)obj).Values?.Equals(Values) ?? false);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public class HackParameterValue
{
    public string Name { get; set; }
    public string Value { get; set; }

    public override bool Equals(object obj)
    {
        return (((HackParameterValue)obj).Name?.Equals(Name) ?? false) && (((HackParameterValue)obj).Value?.Equals(Value) ?? false);
    }
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public enum HackLocation
{
    ARM9 = -1,
    Overlay_00,
    Overlay_01,
    Overlay_02,
    Overlay_03,
    Overlay_04,
    Overlay_05,
    Overlay_06,
    Overlay_07,
    Overlay_08,
    Overlay_09,
    Overlay_10,
    Overlay_11,
    Overlay_12,
    Overlay_13,
    Overlay_14,
    Overlay_15,
    Overlay_16,
    Overlay_17,
    Overlay_18,
    Overlay_19,
    Overlay_20,
    Overlay_21,
    Overlay_22,
    Overlay_23,
    Overlay_24,
    Overlay_25,
}
