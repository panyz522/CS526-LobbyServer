using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SneakRobber2.Shared
{
    public interface IRpcContext
    {
        public EndPoint RemoteEndpoint { get; set; }
    }
}
