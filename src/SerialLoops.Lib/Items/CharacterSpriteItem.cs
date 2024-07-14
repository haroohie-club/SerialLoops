using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SerialLoops.Lib.Items
{
    public class CharacterSpriteItem(CharacterSprite sprite, CharacterDataFile chrdata, Project project, ILogger log) : Item($"SPR_{project.Characters[(int)sprite.Character].Name}_{chrdata.Sprites.IndexOf(sprite):D3}{(sprite.IsLarge ? "_L" : "")}", ItemType.Character_Sprite), IPreviewableGraphic
    {
        private readonly ILogger _log = log;

        public CharacterSprite Sprite { get; set; } = sprite;
        public CharacterSpriteGraphics Graphics { get; set; } = new(sprite, project.Grp);
        public int Index { get; set; } = chrdata.Sprites.IndexOf(sprite);

        public override void Refresh(Project project, ILogger log)
        {
        }

        public List<(SKBitmap Frame, int Timing)> GetClosedMouthAnimation(Project project)
        {
            return Sprite.GetClosedMouthAnimation(project.Grp, project.MessInfo);
        }

        public List<(SKBitmap Frame, int Timing)> GetLipFlapAnimation(Project project)
        {
            return Sprite.GetLipFlapAnimation(project.Grp, project.MessInfo);
        }

        public SKBitmap GetBaseLayout(Project project)
        {
            List<GraphicsFile> textures = [Graphics.BaseTexture, project.Grp.GetFileByIndex(Sprite.TextureIndex2), project.Grp.GetFileByIndex(Sprite.TextureIndex3)];
            (SKBitmap spriteBitmap, _) = Graphics.BaseLayout.GetLayout(textures, 0, Graphics.BaseLayout.LayoutEntries.Count, darkMode: false, preprocessedList: true);

            return spriteBitmap;
        }

        public List<(SKBitmap Frame, short Timing)> GetEyeFrames()
        {
            return Graphics.EyeAnimation.GetAnimationFrames(Graphics.EyeTexture).Select(g => g.GetImage()).Zip(Graphics.EyeAnimation.AnimationEntries.Select(a => ((FrameAnimationEntry)a).Time)).ToList();
        }

        public List<(SKBitmap Frame, short Timing)> GetMouthFrames()
        {
            return Graphics.MouthAnimation.GetAnimationFrames(Graphics.MouthTexture).Select(g => g.GetImage()).ToList().Zip(Graphics.MouthAnimation.AnimationEntries.Select(a => ((FrameAnimationEntry)a).Time)).ToList();
        }

        public void SetSprite(SKBitmap layoutBitmap, List<(SKBitmap Frame, short Time)> eyeFramesAndTimings, List<(SKBitmap Frame, short Time)> mouthFramesAndTimings, short eyeX, short eyeY, short mouthX, short mouthY)
        {
            List<SKColor> palette = SetBaseLayoutAndReturnPalette(layoutBitmap, _log);
            SetEyeAnimation(eyeFramesAndTimings, palette);
            SetMouthAnimation(mouthFramesAndTimings, palette);
            Graphics.EyeAnimation.AnimationX = eyeX;
            Graphics.EyeAnimation.AnimationY = eyeY;
            Graphics.MouthAnimation.AnimationX = mouthX;
            Graphics.MouthAnimation.AnimationY = mouthY;
        }
        
        public SKBitmap GetPreview(Project project)
        {
            return GetClosedMouthAnimation(project).First().Frame;
        }

        private List<SKColor> SetBaseLayoutAndReturnPalette(SKBitmap layoutBitmap, ILogger log)
        {
            List<LayoutEntry> layoutEntries = [];
            SKBitmap texture = new(256, 128);
            SKCanvas textureCanvas = new(texture);
            short srcX = 0, srcY = 0;
            for (short y = 0; y < texture.Height; y += 32)
            {
                for (short x = 0; x < texture.Width; x += 32)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    // Temporary hack until the lib is updated (i cannot update the lib rn bc i'm on a flight)
                    layoutEntries.Add(new([
                        .. BitConverter.GetBytes((short)0),
                        .. BitConverter.GetBytes((short)0),
                        .. BitConverter.GetBytes((short)0),
                        .. BitConverter.GetBytes(srcX),
                        .. BitConverter.GetBytes(srcY),
                        .. BitConverter.GetBytes((short)32),
                        .. BitConverter.GetBytes((short)32),
                        .. BitConverter.GetBytes(x),
                        .. BitConverter.GetBytes(y),
                        .. BitConverter.GetBytes((short)32),
                        .. BitConverter.GetBytes((short)32),
                        .. BitConverter.GetBytes((short)0),
                        .. BitConverter.GetBytes((uint)SKColors.White),
                    ]));

                    SKRect src = new(srcX, srcY, srcX + 32, srcY + 32);
                    srcX += 32;
                    if (srcX >= layoutBitmap.Width)
                    {
                        srcX = 0;
                        srcY += 32;
                    }
                    SKRect dst = new(x, y, x + 32, y + 32);

                    textureCanvas.DrawBitmap(layoutBitmap, src, dst);

                    if (srcY >= layoutBitmap.Height)
                    {
                        break;
                    }
                }
                if (srcY >= layoutBitmap.Height)
                {
                    break;
                }
            }
            textureCanvas.Flush();
            Graphics.BaseLayout.LayoutEntries = layoutEntries;
            List<SKColor> palette = Helpers.GetPaletteFromImage(texture, 255, log);
            palette.Insert(0, SKColors.Transparent);
            Graphics.BaseTexture.Palette = palette;
            Graphics.BaseTexture.SetImage(texture);
            return Graphics.BaseTexture.Palette;
        }

        private void SetEyeAnimation(List<(SKBitmap Frame, short Time)> framesAndTimings, List<SKColor> palette)
        {
            GraphicsFile newEyeTexture = Graphics.EyeAnimation.SetFrameAnimationAndGetTexture(framesAndTimings, palette);
            newEyeTexture.Index = Graphics.EyeTexture.Index;
            Graphics.EyeTexture = newEyeTexture;
        }

        private void SetMouthAnimation(List<(SKBitmap Frame, short Time)> framesAndTimings, List<SKColor> palette)
        {
            GraphicsFile newMouthTexture = Graphics.MouthAnimation.SetFrameAnimationAndGetTexture(framesAndTimings, palette);
            newMouthTexture.Index = Graphics.MouthTexture.Index;
            Graphics.MouthTexture = newMouthTexture;
        }
    }

    public class CharacterSpriteGraphics(CharacterSprite sprite, ArchiveFile<GraphicsFile> grp)
    {
        public GraphicsFile BaseLayout { get; set; } = grp.GetFileByIndex(sprite.LayoutIndex);
        public GraphicsFile BaseTexture { get; set; } = grp.GetFileByIndex(sprite.TextureIndex1);
        public GraphicsFile EyeAnimation { get; set; } = grp.GetFileByIndex(sprite.EyeAnimationIndex);
        public GraphicsFile EyeTexture { get; set; } = grp.GetFileByIndex(sprite.EyeTextureIndex);
        public GraphicsFile MouthAnimation { get; set; } = grp.GetFileByIndex(sprite.MouthAnimationIndex);
        public GraphicsFile MouthTexture { get; set; } = grp.GetFileByIndex(sprite.MouthTextureIndex);

        public void Write(Project project, ILogger log)
        {
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{BaseLayout.Index:X3}.lay"), BaseLayout.GetBytes(), project, log);
            using MemoryStream mainTextureStream = new();
            BaseTexture.GetImage().Encode(mainTextureStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{BaseTexture.Index:X3}.png"), mainTextureStream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{BaseTexture.Index:X3}_pal.csv"), string.Join(',', BaseTexture.Palette.Select(c => c.ToString())), project, log);

            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{EyeAnimation.Index:X3}.bna"), EyeAnimation.GetBytes(), project, log);
            using MemoryStream eyeTextureStream = new();
            EyeTexture.GetImage().Encode(eyeTextureStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{EyeTexture.Index:X3}.png"), eyeTextureStream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{EyeTexture.Index:X3}_pal.csv"), string.Join(',', EyeTexture.Palette.Select(c => c.ToString())), project, log);

            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{MouthAnimation.Index:X3}.bna"), MouthAnimation.GetBytes(), project, log);
            using MemoryStream mouthTextureStream = new();
            MouthTexture.GetImage().Encode(mouthTextureStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{MouthTexture.Index:X3}.png"), mouthTextureStream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{MouthTexture.Index:X3}_pal.csv"), string.Join(',', MouthTexture.Palette.Select(c => c.ToString())), project, log);
        }
    }
}
