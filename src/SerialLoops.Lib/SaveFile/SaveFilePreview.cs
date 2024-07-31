using System;
using HaruhiChokuretsuLib.Save;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SkiaSharp;

namespace SerialLoops.Lib.SaveFile
{
    public class SaveFilePreview(SaveSlotData slotData, Project project)
    {
        private const int WIDTH = 250;
        private const int HEIGHT = 42;
        private const float SCALE = 2f;

        private readonly SaveSlotData _slotData = slotData;
        private readonly Project _project = project;

        public SKBitmap DrawPreview()
        {
            SKBitmap bitmap = new(WIDTH, HEIGHT);

            using SKCanvas canvas = new(bitmap);
            DrawBox(canvas);
            if (_slotData.EpisodeNumber == 0)
            {
                DrawText(canvas, _project.UiText.Messages[27]);
            }
            else
            {
                DrawEpisodeNumber(canvas, _slotData.EpisodeNumber);
                DrawText(canvas, GetEpisodeTitle(_slotData.EpisodeNumber), 90);
                DrawSaveTime(canvas, _slotData.SaveTime);
            }

            canvas.Flush();
            return bitmap.Resize(
                new SKImageInfo(
                    (int)(WIDTH * SCALE),
                    (int)(HEIGHT * SCALE)
                ),
                SKFilterQuality.None
            );
        }

        private void DrawText(SKCanvas canvas, string text, int x = 10, int y = 5, SKPaint paint = null)
        {
            canvas.DrawHaroohieText(
                _project.LangCode != "ja" ? text.GetOriginalString(_project) : text,
                paint ?? DialogueScriptParameter.Paint00,
                _project,
                x,
                y
            );
        }

        private void DrawBox(SKCanvas canvas)
        {
            // Draw outer dark gray outline
            canvas.Clear(new(93, 93, 93));
            SKRect textArea = new(3, 2, WIDTH - 3, HEIGHT - 2);

            // Draw inner green fill
            using SKPaint paint = new();
            paint.Color = new(12, 36, 20);
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawRect(textArea, paint);

            // Draw inner light grey outline
            paint.Color = new(150, 150, 150);
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1;
            canvas.DrawRect(textArea, paint);
        }

        private void DrawEpisodeNumber(SKCanvas canvas, int number)
        {
            if (_project.Items.Find(item => item.Type == ItemDescription.ItemType.System_Texture
                                           && item.Name == "SYSTEX_SYS_CMN_B38") is not SystemTextureItem graphic)
            {
                DrawText(canvas, string.Format(_project.Localize("EPISODE: {0}"), number));
                return;
            }

            // Draw the top half of the image onto the canvas, treating (0, 255, 0) as transparent
            var bitmap = graphic.Grp.GetImage(transparentIndex: 0);
            SKBitmap episodeNumber = new(bitmap.Width + 8, bitmap.Height / 2);
            SKCanvas episodeCanvas = new(episodeNumber);

            // Draw "EPISODE: (number)"
            var numberXOffSet = 8 * (number - 1);
            episodeCanvas.DrawBitmap(bitmap, 0, 0);
            episodeCanvas.DrawBitmap(
                bitmap,
                new SKRectI(numberXOffSet, bitmap.Height / 2, numberXOffSet + 8, bitmap.Height),
                new SKRectI(bitmap.Width - 8, 0, bitmap.Width, bitmap.Height / 2)
            );
            episodeCanvas.Flush();

            canvas.DrawBitmap(episodeNumber, 10, 5);
        }

        private void DrawSaveTime(SKCanvas canvas, DateTimeOffset saveTime)
        {
            string date = saveTime.ToString("yyyy/MM/dd");
            string time = saveTime.ToString("HH:mm:ss");
            if (_project.Items.Find(item => item.Type == ItemDescription.ItemType.System_Texture
                                           && item.Name == "SYSTEX_SYS_MNU_B00") is not SystemTextureItem graphic)
            {
                DrawText(canvas, date, 60, 25, DialogueScriptParameter.Paint01);
                DrawText(canvas, time, 180, 25, DialogueScriptParameter.Paint01);
                return;
            }
            SKBitmap bitmap = graphic.Grp.GetImage(transparentIndex: 0);
            SKBitmap timeBitmap = new(WIDTH, 16);
            SKCanvas timeCanvas = new(timeBitmap);
            // Draw date, for each char
            for (int i = 0; i < date.Length; i++)
            {
                DrawLargeGlyph(date[i], timeCanvas, bitmap, 30 + i * 8);
            }
            for (int i = 0; i < time.Length; i++)
            {
                DrawLargeGlyph(time[i], timeCanvas, bitmap, 160 + i * 8);
            }
            timeCanvas.Flush();
            canvas.DrawBitmap(timeBitmap, 0, 23);
        }

        private static void DrawLargeGlyph(char glyph, SKCanvas canvas, SKBitmap bitmap, int x, int y = 0)
        {
            int offset = glyph switch
            {
                '0' => 0,
                '1' => 8,
                '2' => 16,
                '3' => 24,
                '4' => 32,
                '5' => 40,
                '6' => 48,
                '7' => 56,
                '8' => 64,
                '9' => 72,
                ':' => 80,
                '/' => 88,
                '%' => 96,
                _ => 104
            };
            canvas.DrawBitmap(
                bitmap,
                new SKRectI(offset, 0, offset + 8, bitmap.Height),
                new SKRectI(x, y, x + 8, y + bitmap.Height)
            );
        }

        private string GetEpisodeTitle(int number)
        {
            return _project.UiText.Messages[16 + number];
        }
    }
}
