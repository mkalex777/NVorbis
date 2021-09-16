using System;
using System.IO;

namespace TestApp
{
    class ForwardOnlyStream : Stream
    {
        private readonly Stream _steam;

        public override bool CanRead { get { return _steam.CanRead; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get {  throw new NotSupportedException(); } }

        public override long Position { get { return _steam.Position; } set { throw new NotSupportedException(); } }

        public ForwardOnlyStream(Stream stream)
        {
            _steam = stream;
        }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _steam.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
