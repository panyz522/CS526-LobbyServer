using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SneakRobber2.Utility 
{ 
    public static class CmdHelper
    {
        public static string GetArg(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == key)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
