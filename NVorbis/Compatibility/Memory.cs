using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NVorbis
{
#if NETFX_40
    class Memory<T>
    {
        private readonly Span<T> _span;
        
        public Memory(T[] array)
            : this(new Span<T>(array))
        {
        }

        public Memory(T[] array, int offset, int length)
            : this(new Span<T>(array, offset, length))
        {
        }

        public Memory(Span<T> span)
        {
            this._span = span;
        }

        public int Length { get { return _span.Length; } }

        public Span<T> Span { get { return _span; } }

        public static readonly Memory<T> Empty = new Memory<T>(new T[0]);

        public void CopyTo(Memory<T> target)
        {
            _span.CopyTo(target._span);
        }

        public Memory<T> Slice(int start)
        {
            return new Memory<T>(_span.Slice(start));
        }

        public Memory<T> Slice(int start, int length)
        {
            return new Memory<T>(_span.Slice(start, length));
        }
    }
#endif //NETFX_40
}
