using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Server
{

    class Program
    {
        /// <summary>
        /// Nested class contains Task + Cancellation Token to stop Task when client disconnected
        /// and start new task
        /// </summary>
        private class PipeServer
        {
            public Task ServerTask { get; private set; }
            public CancellationTokenSource CancellationToken { get; private set; }

            public PipeServer(Task task, CancellationTokenSource token)
            {
                ServerTask = task;
                CancellationToken = token;
            }
        }

        /// <summary>
        /// The number of server threads.
        /// </summary>
        private static int _serverThreads = 5;

        /// <summary>
        /// The servers array
        /// </summary>
        private static PipeServer[] _servers = new PipeServer[_serverThreads];
        
        /// <summary>
        /// Message collection
        /// </summary>
        private static BlockingCollection<string> _messages = new BlockingCollection<string>();

        static void Main(string[] args)
        {

            // start server tasks
            for (int i = 0; i < _serverThreads; i++)
            {
                var ts = new CancellationTokenSource();
                CancellationToken ct = ts.Token;
                var task = Task.Factory.StartNew(StartServer, ct);
                _servers[i] = new PipeServer(task, ts);
            }

            // find finished task and restart
            while (true)
            {
                for (int i = 0; i < _serverThreads; i++)
                {
                    if ((_servers[i].ServerTask) != null && (_servers[i].ServerTask.Status == TaskStatus.RanToCompletion))
                    {
                        _servers[i].CancellationToken.Cancel();
                        Thread.Sleep(250);
                        var ts = new CancellationTokenSource();
                        CancellationToken ct = ts.Token;
                        var task = Task.Factory.StartNew(StartServer, ct);
                        _servers[i] = new PipeServer(task, ts);
                    }
                }
            }
        }

        /// <summary>
        /// Server task
        /// </summary>
        static void StartServer()
        {
            NamedPipeServerStream server = new NamedPipeServerStream("PipesOfPiece", PipeDirection.InOut, _serverThreads, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            int threadId = Thread.CurrentThread.ManagedThreadId;

            Console.WriteLine("Waiting for connetion on thread " + threadId);
            server.WaitForConnection();

            StreamWriter writer = new StreamWriter(server);

            writer.WriteLine("Welcome to server on thread " + threadId);
            writer.Flush();
            server.WaitForPipeDrain();
            StreamReader reader = new StreamReader(server);
            string user = "";
            user = reader.ReadLine();
            Console.WriteLine("");
            Console.WriteLine(user + " connected");

            //sending message history
            int maxMessages = 10;
            if (_messages.Count() < maxMessages)
            {
                maxMessages = _messages.Count();
            }
            try
            {
                for (int i = 0; i < maxMessages; i++)
                {
                    var message = _messages.Reverse().ToArray()[maxMessages-i-1];
                    writer.WriteLine(message);
                    writer.Flush();
                    server.WaitForPipeDrain();


                }
                writer.WriteLine("!");//End Message History
                writer.Flush();
                server.WaitForPipeDrain();
            }
            catch (Exception e)
            {
                Console.WriteLine(user + " Piper error " + e.Message);
            }


            int messagesCount; // variable to check for new _messages
            // start reading task
            var ts = new CancellationTokenSource();
            CancellationToken ct = ts.Token;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (true)
                    {
                        var line = reader.ReadLine();
                        if (!String.IsNullOrEmpty(line))
                        {
                            _messages.TryAdd(line);
                            Console.WriteLine(line);
                            messagesCount = _messages.Count();
                        }
                        else
                        {
                            reader.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(user + " Pipe error " + e.Message);
                        }
                    },
                ct);

            // start whatching for new _messages
            messagesCount = _messages.Count();
            try
            {
                while (true)
                {
                    if (_messages.Count() > messagesCount)
                    {
                        for (int i = messagesCount; i < _messages.Count; i++)
                        {
                            writer.WriteLine(_messages.ToArray()[i]);
                            writer.Flush();
                        }
                        server.WaitForPipeDrain();
                        messagesCount = _messages.Count();
                    }
                    Thread.Sleep(1000);

                    //randomly kill server
                    Random rnd = new Random();
                    if (rnd.Next(1, 50) == 10)
                    {
                        writer.WriteLine("Server Over");
                        writer.Flush();
                        ts.Cancel();
                        server.Close();
                        server.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine(user + " Disconnected");
                ts.Cancel();
                server.Close();
                server.Dispose();
            }
        }
    }
}
