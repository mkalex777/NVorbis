﻿using NVorbis.Contracts.Ogg;
using System;
using System.Collections.Generic;

namespace NVorbis.Ogg
{
    class StreamPageReader : IStreamPageReader
    {
        internal static Contracts.IPacketProvider CreatePacketProvider(IStreamPageReader pr, int ss)
        {
            return new PacketProvider(pr, ss);
        }

        private readonly IPageData _reader;
        private readonly List<long> _pageOffsets = new List<long>();

        private int _lastSeqNbr;
        private int? _firstDataPageIndex;
        private long _maxGranulePos;

        private int _lastPageIndex = -1;
        private long _lastPageGranulePos;
        private bool _lastPageIsResync;
        private bool _lastPageIsContinuation;
        private bool _lastPageIsContinued;
        private int _lastPagePacketCount;
        private int _lastPageOverhead;

        private Memory<byte>[] _cachedPagePackets;

        public Contracts.IPacketProvider PacketProvider { get; private set; }

        public StreamPageReader(IPageData pageReader, int streamSerial)
        {
            _reader = pageReader;

            // The packet provider has a reference to us, and we have a reference to it.
            // The page reader has a reference to us.
            // The container reader has a _weak_ reference to the packet provider.
            // The user has a reference to the packet provider.
            // So long as the user doesn't drop their reference and the page reader doesn't drop us,
            //  the packet provider will stay alive.
            // This is important since the container reader only holds a week reference to it.
            PacketProvider = CreatePacketProvider(this, streamSerial);
        }

        public void AddPage()
        {
            // verify we haven't read all pages
            if (!HasAllPages)
            {
                // verify the new page's flags

                // if the page's granule position is -1 that mean's it doesn't have any samples
                if (_reader.GranulePosition != -1)
                {
                    if (_maxGranulePos > _reader.GranulePosition && _maxGranulePos > 0)
                    {
                        // uuuuh, what?!
                        throw new System.IO.InvalidDataException("Granule Position regressed?!");
                    }
                    _maxGranulePos = _reader.GranulePosition;
                }
                // granule position == -1, so this page doesn't complete any packets
                // we don't really care if it's a continuation itself, only that it is continued and has a single packet
                else if (!_reader.IsContinued || _reader.PacketCount != 1)
                {
                    throw new System.IO.InvalidDataException("Granule Position was -1 but page does not have exactly 1 continued packet.");
                }

                if ((_reader.PageFlags & PageFlags.EndOfStream) != 0)
                {
                    HasAllPages = true;
                }

                if (_firstDataPageIndex == null && _reader.GranulePosition > 0)
                {
                    _firstDataPageIndex = _pageOffsets.Count;
                }

                if (_reader.IsResync.Value || (_lastSeqNbr != 0 && _lastSeqNbr + 1 != _reader.SequenceNumber))
                {
                    // as a practical matter, if the sequence numbers are "wrong", our logical stream is now out of sync
                    // so whether the page header sync was lost or we just got an out of order page / sequence jump, we're counting it as a resync
                    _pageOffsets.Add(-_reader.PageOffset);
                }
                else
                {
                    _pageOffsets.Add(_reader.PageOffset);
                }

                _lastSeqNbr = _reader.SequenceNumber;
            }
        }

        public Memory<byte>[] GetPagePackets(int pageIndex)
        {
            if (_cachedPagePackets != null && _lastPageIndex == pageIndex)
            {
                return _cachedPagePackets;
            }

            var pageOffset = _pageOffsets[pageIndex];
            if (pageOffset < 0)
            {
                pageOffset = -pageOffset;
            }

            _reader.Lock();
            try
            {
                _reader.ReadPageAt(pageOffset);
                var packets = _reader.GetPackets();
                if (pageIndex == _lastPageIndex)
                {
                    _cachedPagePackets = packets;
                }
                return packets;
            }
            finally
            {
                _reader.Release();
            }
        }

        public int FindPage(long granulePos)
        {
            // if we're being asked for the first granule, just grab the very first data page
            int pageIndex = -1;
            if (granulePos == 0)
            {
                pageIndex = _firstDataPageIndex ?? FindPageForward(0, 0, 1);
            }
            else
            {
                // start by looking at the last read page's position...
                var lastPageIndex = _pageOffsets.Count - 1;
                long pageGP;
                if (GetPageRaw(lastPageIndex, out pageGP))
                {
                    // most likely, we can look at previous pages for the appropriate one...
                    if (granulePos < pageGP)
                    {
                        //pageIndex = FindPageBisection(granulePos, _firstDataPageIndex ?? 0, lastPageIndex);
                        pageIndex = FindPageBisection(granulePos, _firstDataPageIndex ?? 0, lastPageIndex, pageGP);
                    }
                    // unless we're seeking forward, which is merely an excercise in reading forward...
                    else if (granulePos > pageGP)
                    {
                        pageIndex = FindPageForward(lastPageIndex, pageGP, granulePos);
                    }
                    // but of course, it's possible (though highly unlikely) that the last read page ended on the granule we're looking for.
                    else
                    {
                        pageIndex = lastPageIndex;
                    }
                }
            }
            if (pageIndex == -1)
            {
                throw new ArgumentOutOfRangeException("granulePos");
            }
            return pageIndex;
        }

        private int FindPageForward(int pageIndex, long pageGranulePos, long granulePos)
        {
            while (pageGranulePos < granulePos)
            {
                if (++pageIndex == _pageOffsets.Count)
                {
                    if (!GetNextPageGranulePos(out pageGranulePos))
                    {
                        return -1;
                    }
                }
                else
                {
                    if (!GetPageRaw(pageIndex, out pageGranulePos))
                    {
                        return -1;
                    }
                }
            }
            return pageIndex;
        }

        private bool GetNextPageGranulePos(out long granulePos)
        {
            var pageCount = _pageOffsets.Count;
            while (pageCount == _pageOffsets.Count && !HasAllPages)
            {
                _reader.Lock();
                try
                {
                    if (!_reader.ReadNextPage())
                    {
                        HasAllPages = true;
                        continue;
                    }

                    if (pageCount < _pageOffsets.Count)
                    {
                        granulePos = _reader.GranulePosition;
                        return true;
                    }
                }
                finally
                {
                    _reader.Release();
                }
            }
            granulePos = 0;
            return false;
        }

        private int FindPageBisection(long granulePos, int low, int high, long highGranulePos)
        {
            // we can treat low as always being before the first sample; later work will correct that if needed
            var lowGranulePos = 0L;
            int dist;
            while ((dist = high - low) > 0)
            {
                // try to find the right page by assumming they are all about the same size
                var index = low + (int)(dist * ((granulePos - lowGranulePos) / (double)(highGranulePos - lowGranulePos)));

                // go get the actual position of the selected page
                long idxGranulePos;
                if (!GetPageRaw(index, out idxGranulePos))
                {
                    return -1;
                }

                // figure out where to go from here
                if (idxGranulePos > granulePos)
                {
                    // we read a page after our target (could be the right one, but we don't know yet)
                    high = index;
                    highGranulePos = idxGranulePos;
                }
                else if (idxGranulePos < granulePos)
                {
                    // we read a page before our target
                    low = index + 1;
                    lowGranulePos = idxGranulePos + 1;
                }
                else
                {
                    // direct hit
                    return index;
                }
            }
            return low;
        }

        private bool GetPageRaw(int pageIndex, out long pageGranulePos)
        {
            var offset = _pageOffsets[pageIndex];
            if (offset < 0)
            {
                offset = -offset;
            }

            _reader.Lock();
            try
            {
                if (_reader.ReadPageAt(offset))
                {
                    pageGranulePos = _reader.GranulePosition;
                    return true;
                }
                pageGranulePos = 0;
                return false;
            }
            finally
            {
                _reader.Release();
            }
        }

        public bool GetPage(int pageIndex, out long granulePos, out bool isResync, out bool isContinuation, out bool isContinued, out int packetCount, out int pageOverhead)
        {
            if (_lastPageIndex == pageIndex)
            {
                granulePos = _lastPageGranulePos;
                isResync = _lastPageIsResync;
                isContinuation = _lastPageIsContinuation;
                isContinued = _lastPageIsContinued;
                packetCount = _lastPagePacketCount;
                pageOverhead = _lastPageOverhead;
                return true;
            }

            _reader.Lock();
            try
            {
                while (pageIndex >= _pageOffsets.Count && !HasAllPages)
                {
                    if (_reader.ReadNextPage())
                    {
                        // if we found our page, return it from here so we don't have to do further processing
                        if (pageIndex < _pageOffsets.Count)
                        {
                            isResync = _reader.IsResync.Value;
                            ReadPageData(pageIndex, out granulePos, out isContinuation, out isContinued, out packetCount, out pageOverhead);
                            return true;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                _reader.Release();
            }

            if (pageIndex < _pageOffsets.Count)
            {
                var offset = _pageOffsets[pageIndex];
                if (offset < 0)
                {
                    isResync = true;
                    offset = -offset;
                }
                else
                {
                    isResync = false;
                }

                _reader.Lock();
                try
                {
                    if (_reader.ReadPageAt(offset))
                    {
                        _lastPageIsResync = isResync;
                        ReadPageData(pageIndex, out granulePos, out isContinuation, out isContinued, out packetCount, out pageOverhead);
                        return true;
                    }
                }
                finally
                {
                    _reader.Release();
                }
            }

            granulePos = 0;
            isResync = false;
            isContinuation = false;
            isContinued = false;
            packetCount = 0;
            pageOverhead = 0;
            return false;
        }

        private void ReadPageData(int pageIndex, out long granulePos, out bool isContinuation, out bool isContinued, out int packetCount, out int pageOverhead)
        {
            _cachedPagePackets = null;
            _lastPageGranulePos = granulePos = _reader.GranulePosition;
            _lastPageIsContinuation = isContinuation = (_reader.PageFlags & PageFlags.ContinuesPacket) != 0;
            _lastPageIsContinued = isContinued = _reader.IsContinued;
            _lastPagePacketCount = packetCount = _reader.PacketCount;
            _lastPageOverhead = pageOverhead = _reader.PageOverhead;
            _lastPageIndex = pageIndex;
        }

        public void SetEndOfStream()
        {
            HasAllPages = true;
        }

        public int PageCount { get { return _pageOffsets.Count; } }

        public bool HasAllPages { get; private set; }

        public long? MaxGranulePosition { get { return HasAllPages ? (long?)_maxGranulePos : null; } }

        public int FirstDataPageIndex { get { return _firstDataPageIndex ?? (_firstDataPageIndex = FindPageForward(0, 0, 1)).Value; } }
    }
}
