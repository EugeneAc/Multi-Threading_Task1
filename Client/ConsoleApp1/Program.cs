using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;


namespace Client
{
    using ConsoleApp1;

    class Program
    {
        private static Random rnd = new Random();
        //client
        static void Main(string[] args)
        {
        string[] usernames = new string[] { "gloworella", "wabashglossop", "windywabash", "fortyorella", "emralorella", "bevvyorella", "orellamoonraker", "wabashflorence", "orellaverde", "throwwabash" };
        //Client();
        var client1 = new Client(usernames[rnd.Next(10)]);

            while (true)
            {
                
            }
        }
    }
}