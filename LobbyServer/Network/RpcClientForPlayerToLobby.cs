using Castle.DynamicProxy;
using SneakRobber2.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SneakRobber2.Network
{
    public class RpcClientForPlayerToLobby<TExecutor> : RpcClient
        where TExecutor : ILobbyToPlayer, IRpcData, new()
    {
        public IPlayerToLobby Invoker { get; private set; }

        public RpcClientForPlayerToLobby()
        {
            IProxyGenerator gen = new ProxyGenerator();
            Invoker = gen.CreateInterfaceProxyWithoutTarget<IPlayerToLobby>(new Interceptor(this));
        }

        protected override void OnReceivedData(EndPoint endPoint, string func, object[] ps)
        {
            var callObj = new TExecutor();
            callObj.RemoteEndpoint = endPoint;
            typeof(TExecutor).GetMethod(func).Invoke(callObj, ps);
        }

        private class Interceptor : IInterceptor
        {
            readonly RpcClientForPlayerToLobby<TExecutor> client;

            public Interceptor(RpcClientForPlayerToLobby<TExecutor> client)
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
