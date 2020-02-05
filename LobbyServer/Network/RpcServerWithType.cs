using Castle.DynamicProxy;
using SneakRobber2.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace SneakRobber2.Network
{
    /// <summary>
    /// RPC server.
    /// </summary>
    /// <typeparam name="TExecutor">The type of the executor. Executor is called when server received a RPC from a client.</typeparam>
    /// <typeparam name="IExecutorRpc">The type of the executor RPC.</typeparam>
    /// <typeparam name="IInvokerRpc">The type of the invoker RPC.</typeparam>
    /// <seealso cref="SneakRobber2.Network.RpcServer" />
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

        /// <summary>
        /// Invokes to a client.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <returns>The invoker.</returns>
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
