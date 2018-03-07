namespace ConsoleApp2
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO.Pipes;
    using System.Linq;

    public class ServerManager
    {
        private readonly int _serverThreads;

        private readonly BlockingCollection<Server> _servers = new BlockingCollection<Server>();

        // Message collection
        private List<string> _messages = new List<string>();
        
        private int _maxMessages = 20;

        private bool _keepCreatingNewServer;

        public ServerManager(int numServerThreads)
        {
            this._serverThreads = numServerThreads;

            this.CreateNewServer();

            while (true)
            {
                foreach (var srv in this._servers)
                {
                    if ((srv != null) && srv.ServerThread != null
                                      && (srv.ServerThread.IsAlive == false))
                    {
                        var s = srv;
                        this._servers.TryTake(out s);
                    }
                }

                if (_keepCreatingNewServer)
                { 
                    var server =  this.CreateNewServer();
                    if (server!=null)
                        this._keepCreatingNewServer = false;
                }
            }
        }

        public string[] GetMessageHistory(int maxMessages)
        {
            if (this._messages.Count() < maxMessages)
            {
                maxMessages = this._messages.Count();
            }

            var messages = this._messages.Skip(Math.Max(0, this._messages.Count() - maxMessages));
            return messages.ToArray();
        }

        public Server CreateNewServer()
        {
            try
            {
                NamedPipeServerStream serverStream = new NamedPipeServerStream("PipesOfPiece", PipeDirection.InOut, this._serverThreads, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                Console.WriteLine("Creating new server");
                var server = new Server(serverStream, this);
                server.NewMessageReceived += (s, e) =>
                    {
                        this._messages.Add(e.Message);
                        if (this._messages.Count() > this._maxMessages)
                            this._messages = this._messages.Skip(Math.Max(0, this._messages.Count() - _maxMessages))
                                .ToList();
                        foreach (var srv in this._servers)
                        {
                            if ((srv.ConnectedUserName != e.User) && (srv.ServerThread != null))
                            { 
                                srv.SendMessage(e.Message);
                            }
                        }
                    };
                this._servers.TryAdd(server);
                return server;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("Unable to create new server " + e);
                this._keepCreatingNewServer = true;
                Console.ForegroundColor = ConsoleColor.Green;
            }
            return null;
        }
    }
}
