using System;
using System.Collections.Generic;
using System.Text;

namespace SneakRobber2.Utility
{
    public class EventArgs<T1, T2> : EventArgs
    {
        public T1 V1 { get; set; }
        public T2 V2 { get; set; }

        public EventArgs(T1 v1, T2 v2)
        {
            V1 = v1;
            V2 = v2;
        }
    }
}
