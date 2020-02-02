using System;
using System.Collections.Generic;
using System.Text;

namespace SneakRobber2.Utility
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
