//#define FORCE_USE_DYNAMIC
//#define FORCE_USE_CLASS

#if FORCE_USE_DYNAMIC
#define USE_DYNAMIC
#elif FORCE_USE_CLASS || UNITY_IOS
#define USE_CLASS
#else
#define USE_PROXY
#endif

#if USE_PROXY
using Castle.DynamicProxy;
#endif
using SneakRobber2.Shared;
using SneakRobber2.Utility;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
#if USE_DYNAMIC
        public dynamic Invoker { get; private set; }
#elif USE_PROXY || USE_CLASS
        public IInvokerRpc Invoker { get; private set; }
#endif

        public RpcClientWithType()
        {
#if USE_DYNAMIC
            Invoker = new DynamicRpcInvoker(this);
#elif USE_PROXY
            IProxyGenerator gen = new ProxyGenerator();
            Invoker = gen.CreateInterfaceProxyWithoutTarget<IInvokerRpc>(new Interceptor(this));
#else
            Invoker = (IInvokerRpc)(object)new PlayerToLobbyInvoker(this);  // Only for player to lobby
#endif
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

#if USE_DYNAMIC
        public class DynamicRpcInvoker : DynamicObject
        {
            readonly RpcClientWithType<TExecutor, IExecutorRpc, IInvokerRpc> client;

            public DynamicRpcInvoker(RpcClientWithType<TExecutor, IExecutorRpc, IInvokerRpc> client)
            {
                this.client = client;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                client.Send(binder.Name, args);
                result = null;
                return true;
            }
        }
#elif USE_PROXY
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
#elif USE_CLASS
        public class PlayerToLobbyInvoker : IPlayerToLobby
        {
            private RpcClient client;

            public PlayerToLobbyInvoker(RpcClient client)
            {
                this.client = client;
            }

            public void ChangeName(string name)
            {
                client.Send(RuntimeUtility.GetCallerName(), new object[] { name });
            }

            public void ChangeRoom(string room)
            {
                client.Send(RuntimeUtility.GetCallerName(), new object[] { room });
            }

            public void Exit()
            {
                client.Send(RuntimeUtility.GetCallerName(), new object[] { });
            }

            public void Prepare()
            {
                client.Send(RuntimeUtility.GetCallerName(), new object[] { });
            }

            public void Unprepare()
            {
                client.Send(RuntimeUtility.GetCallerName(), new object[] { });
            }
        }
#endif
    }
}
