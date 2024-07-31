using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Util;
using SerialLoops.Lib.Util;
using SkiaSharp;

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
            return Sprite.GetClosedMouthAnimation(project.MessInfo, Graphics.BodyLayout, Graphics.BodyTextures, Graphics.EyeAnimation, Graphics.EyeTexture, Graphics.MouthAnimation, Graphics.MouthTexture);
        }

        public List<(SKBitmap Frame, int Timing)> GetLipFlapAnimation(Project project)
        {
            return Sprite.GetLipFlapAnimation(project.MessInfo, Graphics.BodyLayout, Graphics.BodyTextures, Graphics.EyeAnimation, Graphics.EyeTexture, Graphics.MouthAnimation, Graphics.MouthTexture);
        }

        public SKBitmap GetBaseLayout()
        {
            return Graphics.BodyLayout.GetLayout(Graphics.BodyTextures, 0, Graphics.BodyLayout.LayoutEntries.Count, darkMode: false, preprocessedList: true).bitmap;
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
            List<SKColor> palette = Helpers.GetPaletteFromImage(layoutBitmap, 255, log);
            palette.Insert(0, SKColors.Transparent);
            short srcX = 0, srcY = 0;

            List<(int W, int H)> sizes = [(256, 128), (256, 32)];
            if (Sprite.IsLarge)
            {
                sizes.Add((256, 64));
            }
            else
            {
                sizes.Add((128, 32));
            }

            for (short i = 0; i < 3; i++)
            {
                SKBitmap texture = new(sizes[i].W, sizes[i].H);
                if (srcY >= layoutBitmap.Height)
                {
                    Graphics.BodyTextures[i].Palette = palette;
                    Graphics.BodyTextures[i].SetImage(texture, newSize: true);
                    continue;
                }

                SKCanvas textureCanvas = new(texture);

                for (short y = 0; y < texture.Height; y += 32)
                {
                    for (short x = 0; x < texture.Width; x += 32)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        SKRect src = new(srcX, srcY, srcX + 32, srcY + 32);
                        srcX += 32;
                        if (srcX >= layoutBitmap.Width)
                        {
                            srcX = 0;
                            srcY += 32;
                        }

                        // If it's all transparent pixels, we can skip it but we should maintain our current position
                        bool allTransparent = true;
                        for (int yy = (int)src.Top; yy < (int)src.Bottom; yy++)
                        {
                            for (int xx = (int)src.Left; xx < (int)src.Right; xx++)
                            {
                                if (layoutBitmap.GetPixel(xx, yy).Alpha != 0)
                                {
                                    allTransparent = false;
                                    break;
                                }
                            }
                            if (!allTransparent)
                            {
                                break;
                            }
                        }
                        if (allTransparent)
                        {
                            x -= 32;
                            continue;
                        }

                        SKRect dst = new(x, y, x + 32, y + 32);
                        layoutEntries.Add(new(i, x, y, 32, 32, (short)src.Left, (short)src.Top, 32, 32, SKColors.White));

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
                Graphics.BodyTextures[i].Palette = palette;
                Graphics.BodyTextures[i].SetImage(texture, newSize: true);
            }

            Graphics.BodyLayout.LayoutEntries = layoutEntries;
            return palette;
        }

        private void SetEyeAnimation(List<(SKBitmap Frame, short Time)> framesAndTimings, List<SKColor> palette)
        {
            GraphicsFile newEyeTexture = Graphics.EyeAnimation.SetFrameAnimationAndGetTexture(framesAndTimings, palette);
            GraphicInfo eyeTexInfo = new(Graphics.EyeTexture);
            eyeTexInfo.SetWithoutPalette(newEyeTexture);
            newEyeTexture.Index = Graphics.EyeTexture.Index;
            Graphics.EyeTexture = newEyeTexture;
        }

        private void SetMouthAnimation(List<(SKBitmap Frame, short Time)> framesAndTimings, List<SKColor> palette)
        {
            GraphicsFile newMouthTexture = Graphics.MouthAnimation.SetFrameAnimationAndGetTexture(framesAndTimings, palette);
            GraphicInfo mouthTexInfo = new(Graphics.MouthTexture);
            mouthTexInfo.SetWithoutPalette(newMouthTexture);
            newMouthTexture.Index = Graphics.MouthTexture.Index;
            Graphics.MouthTexture = newMouthTexture;
        }
    }

    public class CharacterSpriteGraphics(CharacterSprite sprite, ArchiveFile<GraphicsFile> grp)
    {
        public GraphicsFile BodyLayout { get; set; } = grp.GetFileByIndex(sprite.LayoutIndex);
        public List<GraphicsFile> BodyTextures { get; set; } = [grp.GetFileByIndex(sprite.TextureIndex1), grp.GetFileByIndex(sprite.TextureIndex2), grp.GetFileByIndex(sprite.TextureIndex3)];
        public GraphicsFile EyeAnimation { get; set; } = grp.GetFileByIndex(sprite.EyeAnimationIndex);
        public GraphicsFile EyeTexture { get; set; } = grp.GetFileByIndex(sprite.EyeTextureIndex);
        public GraphicsFile MouthAnimation { get; set; } = grp.GetFileByIndex(sprite.MouthAnimationIndex);
        public GraphicsFile MouthTexture { get; set; } = grp.GetFileByIndex(sprite.MouthTextureIndex);

        public void Write(Project project, ILogger log)
        {
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{BodyLayout.Index:X3}.lay"), BodyLayout.GetBytes(), project, log);

            foreach (GraphicsFile bodyTexture in BodyTextures)
            {
                using MemoryStream bodyTextureStream = new();
                bodyTexture.GetImage().Encode(bodyTextureStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{bodyTexture.Index:X3}.png"), bodyTextureStream.ToArray(), project, log);
                IO.WriteStringFile(Path.Combine("assets", "graphics", $"{bodyTexture.Index:X3}.gi"), bodyTexture.GetGraphicInfoFile(), project, log);
            }

            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{EyeAnimation.Index:X3}.bna"), EyeAnimation.GetBytes(), project, log);
            using MemoryStream eyeTextureStream = new();
            EyeTexture.GetImage().Encode(eyeTextureStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{EyeTexture.Index:X3}.png"), eyeTextureStream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{EyeTexture.Index:X3}.gi"), EyeTexture.GetGraphicInfoFile(), project, log);

            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{MouthAnimation.Index:X3}.bna"), MouthAnimation.GetBytes(), project, log);
            using MemoryStream mouthTextureStream = new();
            MouthTexture.GetImage().Encode(mouthTextureStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            IO.WriteBinaryFile(Path.Combine("assets", "graphics", $"{MouthTexture.Index:X3}.png"), mouthTextureStream.ToArray(), project, log);
            IO.WriteStringFile(Path.Combine("assets", "graphics", $"{MouthTexture.Index:X3}.gi"), MouthTexture.GetGraphicInfoFile(), project, log);
        }
    }
}
