namespace Server
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            var serverManager = new ServerManager(2);
            serverManager.Start();
            Console.ReadLine();
        }
    }
}
