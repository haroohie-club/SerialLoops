﻿using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace SerialLoops.Lib.Hacks
{
    public class AsmHack
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<InjectionSite> InjectionSites { get; set; }
        public List<HackFile> Files { get; set; }

        public bool Applied(Project project)
        {
            foreach (InjectionSite site in InjectionSites)
            {
                if (site.Code.Equals("ARM9"))
                {
                    using FileStream arm9 = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "arm9.bin"));
                    arm9.Seek(site.Offset + 3, SeekOrigin.Begin);
                    // All BL functions start with 0xEB
                    if (arm9.ReadByte() == 0xEB)
                    {
                        return true;
                    }
                }
                else
                {
                    using FileStream overlay = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "overlay", $"main_{int.Parse(site.Code):X4}.bin"));
                    overlay.Seek(site.Offset + 3, SeekOrigin.Begin);
                    if (overlay.ReadByte() == 0xEB)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Apply(Project project, Config config, ILogger log)
        {
            if (Applied(project))
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
                File.Copy(Path.Combine(config.HacksDirectory, file.File), destination, true);
            }
        }

        public void Revert(Project project, ILogger log)
        {
            bool oneSuccess = false;
            try
            {
                foreach (HackFile file in Files)
                {
                    File.Delete(Path.Combine(project.BaseDirectory, "src", file.Destination));
                    oneSuccess = true;
                }
            }
            catch (IOException)
            {
                // If there's at least one success, we assume that an older version of the hack was applied and we've now rolled it back
                if (!oneSuccess)
                {
                    log.LogError($"Failed to delete files for hack '{Name}' -- this hack is likely applied in the ROM base and can't be disabled.");
                }
            }
        }

        public override bool Equals(object obj)
        {
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

        public override bool Equals(object obj)
        {
            return ((HackFile)obj).File.Equals(File) && ((HackFile)obj).Destination.Equals(Destination) && ((HackFile)obj).Symbols.SequenceEqual(Symbols);
        }

        public override int GetHashCode()
        {
            return File.GetHashCode();
        }
    }
}
