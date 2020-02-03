using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SneakRobber2.Lobby
{
    public class NameGenerator
    {
        private string[] names;
        private int ptr;

        public NameGenerator()
        {
            names = File.ReadAllLines(Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "names.txt"));
        }

        public string Next()
        {
            var name = names[ptr++];
            if (ptr >= names.Length)
            {
                ptr = 0;
            }
            return name;
        }
    }
}
