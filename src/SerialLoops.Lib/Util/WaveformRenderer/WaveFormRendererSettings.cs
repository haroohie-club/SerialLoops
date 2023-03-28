using SkiaSharp;

// Adapted from https://github.com/naudio/NAudio.WaveFormRenderer/blob/master/WaveFormRendererLib/WaveFormRendererSettings.cs
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
    public class WaveFormRendererSettings
    {
        public static readonly WaveFormRendererSettings StandardSettings = new()
        {
            Width = 400,
            TopHeight = 50,
            BottomHeight = 50,
            PixelsPerPeak = 1,
            SpacerPixels = 0,
            TopPeakPaint = new() { Color = new SKColor(230, 0, 83) },
            BottomPeakPaint = new() { Color = new SKColor(179, 0, 64) },
        };

        public WaveFormRendererSettings()
        {
            BackgroundColor = SKColors.Transparent;
        }

        // for display purposes only
        public string Name { get; set; }

        public int Width { get; set; }

        public int TopHeight { get; set; }
        public int BottomHeight { get; set; }
        public int PixelsPerPeak { get; set; }
        public int SpacerPixels { get; set; }
        public virtual SKPaint TopPeakPaint { get; set; }
        public virtual SKPaint TopSpacerPaint { get; set; }
        public virtual SKPaint BottomPeakPaint { get; set; }
        public virtual SKPaint BottomSpacerPaint { get; set; }
        public bool DecibelScale { get; set; }
        public SKColor BackgroundColor { get; set; }
    }
}