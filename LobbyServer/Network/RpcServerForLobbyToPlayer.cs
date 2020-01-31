using Castle.DynamicProxy;
using SneakRobber2.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace SneakRobber2.Network
{
    public class RpcServerForLobbyToPlayer<TExecutor> : RpcServer
        where TExecutor : IPlayerToLobby, IRpcData, new()
    {
        public interface IInvoker : ILobbyToPlayer, IRpcData { }

        private IInvoker invoker;

        private readonly AsyncLocal<EndPoint> localEndPoint = new AsyncLocal<EndPoint>();

        public RpcServerForLobbyToPlayer()
        {
            IProxyGenerator gen = new ProxyGenerator();
            invoker = gen.CreateInterfaceProxyWithoutTarget<IInvoker>(new Interceptor(this));
        }

        protected override void OnReceivedData(EndPoint endPoint, string func, object[] ps)
        {
            var callObj = new TExecutor();
            callObj.RemoteEndpoint = endPoint;
            typeof(TExecutor).GetMethod(func).Invoke(callObj, ps);
        }

        public IInvoker InvokeTo(EndPoint endPoint)
        {
            localEndPoint.Value = endPoint;
            return invoker;
        }

        private class Interceptor : IInterceptor
        {
            readonly RpcServerForLobbyToPlayer<TExecutor> server;

            public Interceptor(RpcServerForLobbyToPlayer<TExecutor> server)
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
