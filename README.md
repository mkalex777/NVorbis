NVorbis-NETFW40    [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/ioctlLR/NVorbis?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
-------

NVorbis-NETFW40 is a port of NVorbis project for .NET Framework 4.0.

NVorbis is a .Net library for decoding Xiph.org Vorbis files. It is designed to run in partial trust environments and does not require P/Invoke or unsafe code. 

Base NVorbis version 0.10.3


To use:

```cs
// add a reference to NVorbis.dll

// this is the simplest usage; see the public classes and constructors for other options
using (var vorbis = new NVorbis.VorbisReader("path/to/file.ogg"))
{
	// get the channels & sample rate
    var channels = vorbis.Channels;
    var sampleRate = vorbis.SampleRate;

    // OPTIONALLY: get a TimeSpan indicating the total length of the Vorbis stream
    var totalTime = vorbis.TotalTime;

	// create a buffer for reading samples
    var readBuffer = new float[channels * sampleRate / 5];	// 200ms

	// get the initial position (obviously the start)
    var position = TimeSpan.Zero;

    // go grab samples
    int cnt;
    while ((cnt = vorbis.ReadSamples(readBuffer, 0, readBuffer.Length, out _)) > 0)
    {
    	// do stuff with the buffer
    	// samples are interleaved (chan0, chan1, chan0, chan1, etc.)
    	// sample value range is -0.99999994f to 0.99999994f unless vorbis.ClipSamples == false
    
    	// OPTIONALLY: get the position we just read through to...
        position = vorbis.TimePosition;
    }
}
```
