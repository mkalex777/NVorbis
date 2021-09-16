using System;
using System.Collections.Generic;
using System.Threading;


namespace NVorbis
{
#if NETFX_40
    static class MonitorEx
    {
        private static readonly Dictionary<int,Dictionary<object, int>> _threadLocks = new Dictionary<int,Dictionary<object,int>>();

        public static void Enter(object obj)
        {
            lock (_threadLocks)
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                if (!_threadLocks.ContainsKey(threadId))
                    _threadLocks[threadId] = new Dictionary<object, int>();
                var locks = _threadLocks[threadId];
                if (!locks.ContainsKey(obj))
                    locks[obj] = 1;
                else
                    locks[obj]++;
            }
            Monitor.Enter(obj);
        }

        public static void Exit(object obj)
        {
            Monitor.Exit(obj);
            lock (_threadLocks)
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                if (!_threadLocks.ContainsKey(threadId))
                    throw new InvalidOperationException();
                var locks = _threadLocks[threadId];
                if (!locks.ContainsKey(obj))
                    throw new InvalidOperationException();
                else
                    locks[obj]--;
                if (locks[obj] == 0)
                    locks.Remove(obj);
                if (locks.Keys.Count == 0)
                    _threadLocks.Remove(threadId);
            }
        }
        
        public static bool IsEntered(object obj)
        {
            lock (_threadLocks)
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                if (!_threadLocks.ContainsKey(threadId))
                    return false;
                var locks = _threadLocks[threadId];
                return locks.ContainsKey(obj);
            }
        }
    }
#else //NETFX_40
    static class MonitorEx
    {
        public static void Enter(object obj)
        {
            Monitor.Enter(obj);
        }
        
        public static void Exit(object obj)
        {
            Monitor.Exit(obj);
        }
        
        public static bool IsEntered(object obj)
        {
            return Monitor.IsEntered(obj);
        }
    }
#endif //NETFX_40
}
