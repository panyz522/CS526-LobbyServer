using Castle.DynamicProxy;
using SneakRobber2.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace SneakRobber2.Network
{
    public class RpcServerWithType<TExecutor, IExecutorRpc, IInvokerRpc> : RpcServer
        where TExecutor : IExecutorRpc, IRpcContext, new()
        where IInvokerRpc : class
    {
        private readonly IInvokerRpc invoker;

        private readonly AsyncLocal<EndPoint> localEndPoint = new AsyncLocal<EndPoint>();

        public RpcServerWithType()
        {
            IProxyGenerator gen = new ProxyGenerator();
            invoker = gen.CreateInterfaceProxyWithoutTarget<IInvokerRpc>(new Interceptor(this));
        }

        protected override void OnReceivedData(EndPoint endPoint, string func, object[] ps)
        {
            var callObj = new TExecutor
            {
                RemoteEndpoint = endPoint
            };
            typeof(TExecutor).GetMethod(func).Invoke(callObj, ps);
        }

        public IInvokerRpc InvokeTo(EndPoint endPoint)
        {
            localEndPoint.Value = endPoint;
            return invoker;
        }

        private class Interceptor : IInterceptor
        {
            readonly RpcServerWithType<TExecutor, IExecutorRpc, IInvokerRpc> server;

            public Interceptor(RpcServerWithType<TExecutor, IExecutorRpc, IInvokerRpc> server)
            {
                this.server = server;
            }

            public void Intercept(IInvocation invocation)
            {
                server.Send(server.localEndPoint.Value, invocation.Method.Name, invocation.Arguments);
            }
        }
    }
}
