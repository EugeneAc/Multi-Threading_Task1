using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Client
{
    class Program
    {
        private static string[] clientmessages = new string[] { "hello", "hello 1", "bye", "Luke I am your father", "Star wars", "phone", "random message", "you're dead", "monitor", "mouse" };
        private static Random rnd = new Random();
        //client
        static void Main(string[] args)
        {

            Client();
            Console.ReadLine();
        }

        private static void Client()
        {
            while (true)
            {
                //Client
                NamedPipeClientStream client =
                    new NamedPipeClientStream(".", "PipesOfPiece",
                        PipeDirection.InOut, PipeOptions.Asynchronous,
                        TokenImpersonationLevel.Delegation);
                Console.WriteLine("Connecting to server...\n");
                client.Connect();
                StreamReader reader = new StreamReader(client);
                StreamWriter writer = new StreamWriter(client);

                Console.WriteLine(reader.ReadLine());

                var username = "Username" + rnd.Next(10);
                writer.WriteLine(username);
                writer.Flush();
                Console.WriteLine("Connected with name " + username);
                client.WaitForPipeDrain();

                Console.WriteLine("Message history");
                Console.ForegroundColor = ConsoleColor.Red;
                bool readHistory = true;
                while (readHistory)
                {
                    var readLine = reader.ReadLine();
                    if (readLine != "!")
                    { 
                        Console.WriteLine(readLine);
                    }
                    else
                    {
                        readHistory = false;
                    }

                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("End message history");
                Console.WriteLine("");

                //strart reading task
                var ts = new CancellationTokenSource();
                CancellationToken ct = ts.Token;
                var readingtask = Task.Factory.StartNew(() =>
                {
                    bool continueRead = true;
                    try
                    {

                        while (continueRead)
                        {
                            var line = reader.ReadLine();

                            if (line != "Server Over") continueRead = false;

                            if (!String.IsNullOrEmpty(line))
                            {
                                Console.WriteLine(line);
                            }
                            else
                            {
                                reader.Close();//close stream when "Server Over"
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Server error :" + e.Message);
                        client.Dispose(); // close stream when server error
                    }
                    Console.WriteLine("Reading Over");
                }, ct);

                //sending random messages
                Console.WriteLine("");
                Thread.Sleep(1000);
                try
                {
                    for (int i = 0; i < rnd.Next(2, 10); i++)
                    {
                        Thread.Sleep(rnd.Next(100, 1000));
                        string phrase = username + " says: " + clientmessages[rnd.Next(10)];
                        Console.WriteLine(phrase);
                        writer.WriteLine(phrase);
                        writer.Flush();
                    }
                    client.WaitForPipeDrain();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Sending error :" + e.Message);
                }
                

                bool clientAlive = true;
                int counter = 0;
                while (clientAlive)
                {
                    if (readingtask.Status == TaskStatus.RanToCompletion)
                    {
                        client.Close();
                        client.Dispose();
                        clientAlive = false;
                        Console.WriteLine("Auto Disconnect");
                    }
                    Thread.Sleep(100);
                    counter++;
                    if (counter == 100) clientAlive = false;
                }

                Console.WriteLine("Contunue? Press Enter");
                Console.ReadLine();
                ts.Cancel();
                client.Close();
                client.Dispose();
            }
        }
    }
}