using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Server
{
    using ConsoleApp2;

    class Program
    {

        static void Main(string[] args)
        {
            var serverManager = new ServerManager(2);
            Console.ReadLine();
        }
    }
}
