using System;
using NAudio.Wave;
using SkiaSharp;

// Adapted from https://github.com/naudio/NAudio.WaveFormRenderer/blob/master/WaveFormRendererLib/WaveFormRenderer.cs
// Copyright (c) 2021 NAudio
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace SerialLoops.Lib.Util.WaveformRenderer
{
    public static class WaveformRenderer
    {
        public static SKBitmap Render(WaveStream waveStream, WaveFormRendererSettings settings)
        {
            int bytesPerSample = waveStream.WaveFormat.BitsPerSample / 8;
            long numSamples = waveStream.Length / bytesPerSample;
            int samplesPerPixel = (int)(numSamples / settings.Width);
            int stepSize = settings.PixelsPerPeak + settings.SpacerPixels;
            MaxPeakProvider peakProvider = new();
            peakProvider.Init(waveStream.ToSampleProvider(), samplesPerPixel * stepSize);
            return Render(peakProvider, settings);
        }

        public static SKBitmap Render(ISampleProvider sampleProvider, long length, WaveFormRendererSettings settings)
        {
            int bytesPerSample = sampleProvider.WaveFormat.BitsPerSample / 8;
            long numSamples = length / bytesPerSample;
            int samplesPerPixel = (int)(numSamples / settings.Width);
            int stepSize = settings.PixelsPerPeak + settings.SpacerPixels;
            MaxPeakProvider peakProvider = new();
            peakProvider.Init(sampleProvider, samplesPerPixel * stepSize);
            return Render(peakProvider, settings);
        }

        private static SKBitmap Render(PeakProvider peakProvider, WaveFormRendererSettings settings)
        {
            SKBitmap waveformBitmap = new(settings.Width, settings.TopHeight + settings.BottomHeight);

            using SKCanvas canvas = new(waveformBitmap);

            if (settings.BackgroundColor != SKColors.Transparent)
            {
                canvas.DrawRect(0, 0, waveformBitmap.Width, waveformBitmap.Height, new SKPaint { Color = settings.BackgroundColor });
            }
            int midpoint = settings.TopHeight;

            int x = 0;
            PeakInfo currentPeak = peakProvider.GetNextPeak();
            while (x < settings.Width)
            {
                PeakInfo nextPeak = peakProvider.GetNextPeak();

                for (int n = 0; n < settings.PixelsPerPeak; n++)
                {
                    float lineHeight = settings.TopHeight * currentPeak.Max;
                    canvas.DrawLine(x, midpoint, x, midpoint - lineHeight, settings.TopPeakPaint);
                    lineHeight = settings.BottomHeight * currentPeak.Min;
                    canvas.DrawLine(x, midpoint, x, midpoint - lineHeight, settings.BottomPeakPaint);
                    x++;
                }

                for (int n = 0; n < settings.SpacerPixels; n++)
                {
                    float max = Math.Max(currentPeak.Max, nextPeak.Max);
                    float min = Math.Min(currentPeak.Min, nextPeak.Min);

                    float lineHeight = settings.TopHeight * max;
                    canvas.DrawLine(x, midpoint, x, midpoint - lineHeight, settings.TopSpacerPaint);
                    lineHeight = settings.BottomHeight * min;
                    canvas.DrawLine(x, midpoint, x, midpoint - lineHeight, settings.BottomSpacerPaint);
                    x++;
                }
                currentPeak = nextPeak;
            }

            return waveformBitmap;
        }
    }
}
