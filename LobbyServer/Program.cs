using Castle.DynamicProxy;
using System;
using System.Dynamic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace LobbyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IProxyGenerator gen = new ProxyGenerator();
            var ic = gen.CreateInterfaceProxyWithoutTarget<ITest>(new Interceptor());
            ic.TestA();
        }
    }

    public interface ITest
    {
        void TestA();
    }

    public class Interceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Console.WriteLine(invocation.Method.Name);
        }
    }
}
