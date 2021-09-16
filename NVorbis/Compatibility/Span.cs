using System;
using System.Collections.Generic;
using System.Linq;


namespace NVorbis
{
#if NETFX_40
    public class Span<T> : IEnumerable<T>
    {
        internal readonly T[] _source;
        internal readonly int _offset;
        internal readonly int _length;

        public static implicit operator Span<T>(T[] src)
        {
            return new Span<T>(src);
        }

        public Span(Span<T> source, int offset, int length)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (offset < 0 || offset > source.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (length < 0 || length > source.Length - offset)
                throw new ArgumentOutOfRangeException("length");
            _source = source._source;
            _offset = source._offset + offset;
            _length = length;
        }

        public Span(T[] source, int offset, int length)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (offset < 0 || offset > source.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (length < 0 || length > source.Length-offset)
                throw new ArgumentOutOfRangeException("length");
            _source = source;
            _offset = offset;
            _length = length;
        }

        public Span(Span<T> source)
            : this(source, 0, source.Length)
        {
        }

        public Span(Span<T> source, int offset)
            : this(source, offset, source.Length - offset)
        {
        }

        public Span(T[] source)
            : this(source, 0, source.Length)
        {
        }

        public Span(T[] source, int offset)
            : this(source, offset, source.Length-offset)
        {
        }

        public int Length { get { return _length; } }

        public T this[int index]
        {
            get { return _source[_offset + CheckRange(index)]; }
            set { _source[_offset + CheckRange(index)] = value; }
        }

        public Span<T> Slice(int start, int length)
        {
            return new Span<T>(this, start, length);
        }

        public Span<T> Slice(int start)
        {
            return new Span<T>(this, start);
        }

        public void Fill(T value)
        {
            for (var i = 0; i < _length; i++)
                _source[_offset + i] = value;
        }

        public void CopyTo(Span<T> destination)
        {
            if (_length > destination._length)
                throw new ArgumentOutOfRangeException("destination");
            Array.Copy(_source, _offset, destination._source, destination._offset, _length);
        }

        public T[] ToArray()
        {
            var result = new T[_length];
            Array.Copy(_source, _offset, result, 0, _length);
            return result;
        }


        private int CheckRange(int index)
        {
            if (index < 0 || index >= _length ||
                _offset + index < 0 || _offset + index >= _source.Length)
                throw new ArgumentOutOfRangeException("index");
            return index;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _source.Skip(_offset).Take(_length).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            var enumerable = _source.Skip(_offset).Take(_length) as System.Collections.IEnumerable;
            return enumerable.GetEnumerator();
        }

        public bool IsEmpty { get { return _length == 0; } }
    }
#endif //NETFX_40
}