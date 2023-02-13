using Eto.Mac.Forms.Controls;
using MonoMac.AppKit;
using NAudio.Wave;
using System;

namespace SerialLoops.Mac
{
    public class SoundPlayerHandler : MacControl<NSButton, SoundPlayer, SoundPlayer.ICallback>, SoundPlayer.ISoundPlayer
    {
        public IWaveProvider WaveProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsPlaying => throw new NotImplementedException();

        public void Initialize(IWaveProvider waveProvider)
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
