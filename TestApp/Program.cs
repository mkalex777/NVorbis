using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

// use NAudio 1.8.0 for FW 4.0
// https://github.com/naudio/NAudio/releases/download/NAudio_1.8_Release/NAudio-1.8.0-Release.zip

namespace TestApp
{
    static class Program
    {
        const string OGG_FILE1 = @"..\..\..\TestFiles\1test.ogg";
        const string OGG_FILE2 = @"..\..\..\TestFiles\2test.ogg";
        const string OGG_FILE3 = @"..\..\..\TestFiles\3test.ogg";

        static void Main()
        {
            Play(OGG_FILE3);
            Play(OGG_FILE2);
            Play(OGG_FILE1);
        }

        private static void Play(string fileName)
        {
            Console.WriteLine("Play {0}...", fileName);
            using (var fs = File.OpenRead(fileName))
            //using (var fwdStream = new ForwardOnlyStream(fs))
            using (var waveStream = new VorbisWaveStream(fs))
            using (var waveOut = new WaveOutEvent())
            {
                var wait = new System.Threading.ManualResetEventSlim(false);
                waveOut.PlaybackStopped += (s, e) => wait.Set();

                waveOut.Init(waveStream);
                waveOut.Play();

                wait.Wait();
            }
        }
    }
}
