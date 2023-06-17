using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;

namespace SerialLoops.Lib.Hacks
{
    public class Hack
    {
        public string Name { get; set; }
        public List<InjectionSite> InjectionSites { get; set; }
        public List<string> Files { get; set; }

        public bool Applied(Project project)
        {
            foreach (InjectionSite site in InjectionSites)
            {
                if (site.Equals("ARM9"))
                {
                    FileStream arm9 = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "arm9.bin"));
                    arm9.Seek(site.Offset + 3, SeekOrigin.Begin);
                    if (arm9.ReadByte() == 0xEB)
                    {
                        return true;
                    }
                }
                else
                {
                    FileStream overlay = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "overlay", $"main_{int.Parse(site.Code):X4}.bin"));
                    overlay.Seek(site.Offset + 3, SeekOrigin.Begin);
                    if (overlay.ReadByte() == 0xEB)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Apply(Project project, ILogger log)
        {
            if (Applied(project))
            {
                log.LogWarning($"Hack '{Name}' already applied, skipping.");
                return;
            }

            foreach (string file in Files)
            {
                IO.CopyFileToDirectories(project, file, Path.Combine("rom", ))
            }
        }
    }

    public class InjectionSite
    {
        [JsonIgnore]
        public uint Offset { get; set; }
        public string Code { get; set; }
        public string Location
        {
            get
            {
                int startAddress;
                if (!Code.Equals("ARM"))
                {
                    startAddress = 0x020C7660;
                }
                else
                {
                    startAddress = 0x01FF8000;
                }
                return $"{(uint)(Offset + startAddress):X8}";
            }
            set
            {
                int startAddress;
                if (!Code.Equals("ARM"))
                {
                    startAddress = 0x020C7660;
                }
                else
                {
                    startAddress = 0x01FF8000;
                }
                Offset = (uint)(uint.Parse(value, System.Globalization.NumberStyles.HexNumber) - startAddress);
            }
        }
    }

    public class HackFile
    {

    }
}
