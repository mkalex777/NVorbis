using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NVorbis
{
#if NETFX_40
    class WeakReference<T>
    {
        private readonly WeakReference _weak;

        public WeakReference(T value)
        {
            _weak = new WeakReference(value);
        }

        public bool TryGetTarget(out T pp)
        {
            pp = default(T);
            if (!_weak.IsAlive) 
                return false;
            pp = (T)_weak.Target;
            return _weak.IsAlive;
        }
    }
#endif //NETFX_40
}
