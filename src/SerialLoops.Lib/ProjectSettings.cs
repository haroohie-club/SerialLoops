using HaroohieClub.NitroPacker.Core;
using HaroohieClub.NitroPacker.Nitro.Gx;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using static HaroohieClub.NitroPacker.Nitro.Card.Rom.RomBanner;
using static HaruhiChokuretsuLib.Archive.Event.VoiceMapFile.VoiceMapStruct;

namespace SerialLoops.Lib
{
    public class ProjectSettings
    {
        public NdsProjectFile File { get; private set; }

        public BannerV1 Banner {
            get => File.RomInfo.Banner.Banner;
            private set
            {
                File.RomInfo.Banner.Banner = value;
            }
        }

        public string Name {
            get
            {
                return Banner.GameName[0];
            }
            set
            {
                string name = value.Length > 128 ? value[..63] : value;
                for (int i = 0; i < Banner.GameName.Length; i++)
                {
                    Banner.GameName[i] = name;
                }
            }
        }

        public SKBitmap Icon
        { 
            get
            {
                Rgba8Bitmap bitmap = Banner.GetIcon();
                return new(bitmap.Width, bitmap.Height)
                {
                    Pixels = bitmap.Pixels.Select(p => new SKColor(p)).ToArray()
                };
            }
            set
            {
                // todo encode into image
                // 512 bytes => image (1b / 2 pixels)
                // 32 bytes => palette (2b / color)
                // see https://github.com/haroohie-club/NitroPacker/blob/master/HaroohieClub.NitroPacker.Nitro/Gx/GxUtil.cs#L264-L276
                // also useful reference: https://github.com/TheGameratorT/NDS_Banner_Editor/blob/master/qndsimage.cpp
            }
        }
        public ProjectSettings(NdsProjectFile file)
        {
            File = file;
        }

    }
}
