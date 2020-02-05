using Castle.DynamicProxy;
using SneakRobber2.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SneakRobber2.Network
{
    /// <summary>
    /// RPC client.
    /// </summary>
    /// <typeparam name="TExecutor">The type of the executor. Executor is called when client received a callback.</typeparam>
    /// <typeparam name="IExecutorRpc">The type of the executor RPC interface.</typeparam>
    /// <typeparam name="IInvokerRpc">The type of the invoker RPC interface.</typeparam>
    /// <seealso cref="SneakRobber2.Network.RpcClient" />
    public class RpcClientWithType<TExecutor, IExecutorRpc, IInvokerRpc> : RpcClient
        where TExecutor : IExecutorRpc, IRpcContext, new()
        where IInvokerRpc : class
    {
        /// <summary>
        /// Gets the invoker. The invoker can be used to call RPC to server.
        /// </summary>
        /// <value>
        /// The invoker.
        /// </value>
        public IInvokerRpc Invoker { get; private set; }

        public RpcClientWithType()
        {
            IProxyGenerator gen = new ProxyGenerator();
            Invoker = gen.CreateInterfaceProxyWithoutTarget<IInvokerRpc>(new Interceptor(this));
        }

        /// <summary>
        /// Called when received data.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <param name="func">The function.</param>
        /// <param name="ps">The ps.</param>
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
