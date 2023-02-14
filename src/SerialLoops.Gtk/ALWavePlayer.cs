using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SerialLoops.Gtk
{
    public class ALWavePlayer : IWavePlayer
    {
        private int sourceID; // source is the sound source, like an ID of the soundcard
        private int bufferCount;
        private int[] bufferIDs; // generating four buffers, so they can be played in sequence
        private int state; // current execution state, should be 4116 for ALSourceState.Stopped and 4114 for ALSourceState.Playing
        private int channels; // how many audio channels to allocate
        private int bitsPerSample; // default bits per sample
        private int sampleRate; // default audio rate
        private byte[] soundData;
        private ALFormat alFormat;
        private int milissecondsPerBuffer;
        private int bufferSize;
        private int currentBufferIndex;
        private Thread playThread;
        private int buffersRead;
        private int buffersPlayed;
        private bool shouldPlay;

        private IWaveProvider waveProvider;

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public PlaybackState PlaybackState
        {
            get
            {
                return PlaybackState.Stopped; // mock
            }
        }

        public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public WaveFormat OutputWaveFormat => throw new NotImplementedException();

        public ALWavePlayer()
        {
        }

        public ALWavePlayer(int latency)
        {
        }

        public void Init(IWaveProvider waveProvider)
        {
            sourceID = AL.GenSource(); // source is the sound source, like an ID of the soundcard
            bufferCount = 3; // tripple buffering, just so we get backed up.
            bufferIDs = AL.GenBuffers(bufferCount); // generating four buffers, so they can be played in sequence
            state = 4116; // current execution state, should be 4116 for ALSourceState.Stopped and 4114 for ALSourceState.Playing
            channels = 2; // how many audio channels to allocate. 1 for mono and 2 for stereo
            bitsPerSample = 16; // default bits per sample
            sampleRate = 44100; // default audio rate
            milissecondsPerBuffer = 1000;
            alFormat = ALWavePlayer.GetSoundFormat(channels, bitsPerSample);
            int sampleSizeInBytes = (bitsPerSample / 8) * channels;
            bufferSize = (int)(sampleSizeInBytes * sampleRate * (milissecondsPerBuffer / 1000f));
            Console.WriteLine("Using buffers of " + (milissecondsPerBuffer / 1000f) + " seconds");
            currentBufferIndex = 0;
            buffersRead = 0;
            buffersPlayed = 0;

            this.waveProvider = waveProvider;
        }

        public void Play()
        {
            playThread = new Thread(new ThreadStart(PlaybackThreadFunc));
            //			playThread = new Thread (new ParameterizedThreadStart(PlaybackThreadFunc));
            //			playThread = new Thread (() => PlaybackThreadFunc (this));
            // put this back to highest when we are confident we don't have any bugs in the thread proc
            playThread.Priority = ThreadPriority.Normal;
            playThread.IsBackground = true;
            shouldPlay = true;
            playThread.Start(this);
        }

        public void Stop()
        {
            AL.SourceStop(sourceID);
            shouldPlay = false;
            Console.WriteLine("Trying to stop play thread");
            Console.WriteLine(shouldPlay);
        }

        public void Dispose()
        {
            AL.SourceStop(sourceID); // stop sound source
            AL.DeleteSource(sourceID); // delete it
            AL.DeleteBuffers(bufferIDs); // and also the buffers
        }

        private void PlaybackThreadFunc()
        {
            for (int i = 0; i < bufferIDs.Length; i++)
            { // fill all the buffers first
                soundData = new byte[bufferSize];
                waveProvider.Read(soundData, 0, soundData.Length);
                AL.BufferData(bufferIDs[i], alFormat, soundData, sampleRate); // put it into the sound buffer
                buffersRead++;
                Console.WriteLine("Queuing chunk " + buffersRead + ".");
            }

            AL.SourceQueueBuffers(sourceID, bufferIDs.Length, bufferIDs);
            AL.SourcePlay(sourceID); // and plays them
            buffersPlayed++;

            do
            {
                Thread.Sleep(100); // wait a little bit
                int buffersProcessed = 0;
                AL.GetSource(sourceID, ALGetSourcei.BuffersProcessed, out buffersProcessed); // verify if some buffer has ended playing
                for (; buffersProcessed > 0; buffersProcessed--)
                {
                    int oldestProcessedBufferID = AL.SourceUnqueueBuffer(sourceID); // unqueue the oldest played buffer
                    buffersRead++;
                    Console.WriteLine("Queuing chunk " + buffersRead + ".");
                    waveProvider.Read(soundData, 0, soundData.Length);
                    AL.BufferData(oldestProcessedBufferID, alFormat, soundData, sampleRate); // put it into the sound buffer
                    AL.SourceQueueBuffer(sourceID, oldestProcessedBufferID); // enqueues the next buffer
                }
                AL.GetSource(sourceID, ALGetSourcei.SourceState, out state); // check the state of the audio source execution
                Console.WriteLine(shouldPlay);
            } while ((ALSourceState)state == ALSourceState.Playing && shouldPlay);
        }

        private static ALFormat GetSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1:
                    return bits <= 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                case 2:
                    return bits <= 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                default:
                    throw new NotSupportedException("The specified sound format is not supported.");
            }
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }
    }

}