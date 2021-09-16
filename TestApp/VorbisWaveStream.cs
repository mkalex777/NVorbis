using NVorbis;
using NVorbis.Contracts;
using System;

namespace TestApp
{
    class VorbisWaveStream : NAudio.Wave.WaveStream, NAudio.Wave.ISampleProvider
    {
        internal static IVorbisReader CreateFileReader(string fn)
        {
            return new VorbisReader(fn);
        }
        internal static IVorbisReader CreateStreamReader(System.IO.Stream ss)
        {
            return new VorbisReader(ss, false);
        }

        private IVorbisReader _reader;

        NAudio.Wave.WaveFormat _waveFormat;

        public event EventHandler ParameterChange;

        public VorbisWaveStream(string fileName)
        {
            _reader = CreateFileReader(fileName);

            UpdateWaveFormat();
        }

        public VorbisWaveStream(System.IO.Stream sourceStream)
        {
            _reader = CreateStreamReader(sourceStream);

            UpdateWaveFormat();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_reader != null)
                    _reader.Dispose();
                _reader = null;
            }

            base.Dispose(disposing);
        }

        public override NAudio.Wave.WaveFormat WaveFormat { get { return _waveFormat; } }

        private void UpdateWaveFormat()
        {
            _waveFormat = NAudio.Wave.WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
            if (ParameterChange != null)
                ParameterChange.Invoke(this, EventArgs.Empty);
        }

        public override long Length { get { return _reader.TotalSamples * _waveFormat.BlockAlign; } }

        public override long Position
        {
            get { return _reader.SamplePosition * _waveFormat.BlockAlign; }
            set { _reader.SamplePosition = value / _waveFormat.BlockAlign; }
        }

        // This buffer can be static because it can only be used by 1 instance per thread
        [ThreadStatic]
        static float[] _conversionBuffer = null;

        public override int Read(byte[] buffer, int offset, int count)
        {
            // adjust count so it is in floats instead of bytes
            count /= sizeof(float);

            // make sure we don't have an odd count
            count -= count % _reader.Channels;

            // get the buffer, creating a new one if none exists or the existing one is too small
            var cb = _conversionBuffer ?? (_conversionBuffer = new float[count]);
            if (cb.Length < count)
            {
                cb = (_conversionBuffer = new float[count]);
            }

            // let Read(float[], int, int) do the actual reading; adjust count back to bytes
            int cnt = Read(cb, 0, count) * sizeof(float);

            // move the data back to the request buffer
            Buffer.BlockCopy(cb, 0, buffer, offset, cnt);

            // done!
            return cnt;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (IsParameterChange) throw new InvalidOperationException("Parameter change pending!  Call ClearParameterChange() before reading more data.");

            var cnt = _reader.ReadSamples(buffer, offset, count);
            if (cnt == 0)
            {
                if (_reader.IsEndOfStream && AutoAdvanceToNextStream)
                {
                    if (_reader.StreamIndex < _reader.Streams.Count - 1)
                    {
                        if (_reader.SwitchStreams(_reader.StreamIndex + 1))
                        {
                            IsParameterChange = true;
                            UpdateWaveFormat();
                            return 0;
                        }
                        else
                        {
                            return Read(buffer, offset, count);
                        }
                    }
                }
            }
            return cnt;
        }

        public bool AutoAdvanceToNextStream { get; set; }

        public bool IsParameterChange { get; private set; }

        public void ClearParameterChange() { IsParameterChange = false; }

        public bool IsEndOfStream { get { return _reader.IsEndOfStream; } }

        public int StreamCount { get { return _reader.Streams.Count; } }

        public int StreamIndex
        {
            get { return _reader.StreamIndex; }
            set
            {
                if (value < 0 || value >= _reader.Streams.Count) 
                    throw new ArgumentOutOfRangeException("value");
                if (_reader.SwitchStreams(value))
                {
                    UpdateWaveFormat();
                }
            }
        }

        public bool FindNewStream() { return _reader.FindNextStream(); }

        public IStreamStats Stats { get { return _reader.StreamStats; } }
        public ITagData Tags { get { return _reader.Tags; } }

        /// <summary>
        /// Gets the encoder's upper bitrate of the current selected Vorbis stream
        /// </summary>
        public int UpperBitrate { get { return _reader.UpperBitrate; } }

        /// <summary>
        /// Gets the encoder's nominal bitrate of the current selected Vorbis stream
        /// </summary>
        public int NominalBitrate { get { return _reader.NominalBitrate; } }

        /// <summary>
        /// Gets the encoder's lower bitrate of the current selected Vorbis stream
        /// </summary>
        public int LowerBitrate { get { return _reader.LowerBitrate; } }
    }
}
