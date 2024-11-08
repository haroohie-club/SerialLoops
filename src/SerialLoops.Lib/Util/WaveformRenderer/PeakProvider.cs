using NAudio.Wave;

// Adapted from https://github.com/naudio/NAudio.WaveFormRenderer/blob/master/WaveFormRendererLib/PeakProvider.cs
// and from https://github.com/naudio/NAudio.WaveFormRenderer/blob/master/WaveFormRendererLib/PeakInfo.cs
// Copyright (c) 2021 NAudio
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace SerialLoops.Lib.Util.WaveformRenderer;

public abstract class PeakProvider
{
    protected ISampleProvider Provider { get; private set; }
    protected int SamplesPerPeak { get; private set; }
    protected float[] ReadBuffer { get; private set; }

    public void Init(ISampleProvider provider, int samplesPerPeak)
    {
        Provider = provider;
        SamplesPerPeak = samplesPerPeak;
        ReadBuffer = new float[samplesPerPeak];
    }

    public abstract PeakInfo GetNextPeak();
}

public class PeakInfo(float min, float max)
{
    public float Min { get; private set; } = min;
    public float Max { get; private set; } = max;
}