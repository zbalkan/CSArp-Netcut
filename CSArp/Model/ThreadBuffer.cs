using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace CSArp.Model
{
    public static class ThreadBuffer
    {
        public static int Count
        {
            get
            {
                return buffer.Count;
            }
        }

        public static int AliveCount
        {
            get
            {
                return buffer.Count(t => t.IsAlive);
            }
        }

        private static List<Thread> buffer;

        public static void Init()
        {
            buffer = new List<Thread>();
        }

        public static void Add(Thread thread)
        {
            buffer.Add(thread);
            thread.Start();
        }

        public static void Remove(Thread thread)
        {
            thread.Abort();
            _ = buffer.Remove(thread);
        }

        public static void Clear()
        {
            foreach (var t in buffer)
            {
                if (t.IsAlive)
                {
                    t.Abort();
                }
            }
            buffer.Clear();
        }
    }
}
