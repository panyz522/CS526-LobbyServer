using Castle.DynamicProxy;
using SneakRobber2.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SneakRobber2.Network
{
    public class RpcClientWithType<TExecutor, IExecutorRpc, IInvokerRpc> : RpcClient
        where TExecutor : IExecutorRpc, IRpcContext, new()
        where IInvokerRpc : class
    {
        public IInvokerRpc Invoker { get; private set; }

        public RpcClientWithType()
        {
            IProxyGenerator gen = new ProxyGenerator();
            Invoker = gen.CreateInterfaceProxyWithoutTarget<IInvokerRpc>(new Interceptor(this));
        }

        protected override void OnReceivedData(EndPoint endPoint, string func, object[] ps)
        {
            var callObj = new TExecutor();
            callObj.RemoteEndpoint = endPoint;
            typeof(TExecutor).GetMethod(func).Invoke(callObj, ps);
        }

        private class Interceptor : IInterceptor
        {
            readonly RpcClientWithType<TExecutor, IExecutorRpc, IInvokerRpc> client;

            public Interceptor(RpcClientWithType<TExecutor, IExecutorRpc, IInvokerRpc> client)
            {
                this.client = client;
            }

            public void Intercept(IInvocation invocation)
            {
                client.Send(invocation.Method.Name, invocation.Arguments);
            }
        }
    }
}
