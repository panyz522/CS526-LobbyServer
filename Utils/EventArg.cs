using System;
using System.Collections.Generic;
using System.Text;

namespace SneakRobber2.Utils
{
    public class EventArg<T> : EventArgs
    {
        public T Value { get; set; }

        public EventArg(T val)
        {
            Value = val;
        }
    }
}
