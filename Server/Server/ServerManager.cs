namespace Server
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

        public ServerManager(int serverThreads)
        {
            _serverThreads = serverThreads;
        }

        public void Start()
        {
            CreateNewServer();

            while (true)
            {
                foreach (var srv in _servers)
                {
                    if ((srv != null) && srv.ServerThread != null
                                      && (srv.ServerThread.IsAlive == false))
                    {
                        var s = srv;
                        srv.NewMessageReceived -= OnServerOnNewMessageReceived;
                        _servers.TryTake(out s);
                    }
                }

                if (_keepCreatingNewServer)
                {
                    var server = CreateNewServer();
                    if (server != null)
                        _keepCreatingNewServer = false;
                }
            }
        }

        public string[] GetMessageHistory(int messageNum)
        {
            if (_messages.Count() < messageNum)
            {
                messageNum = _messages.Count();
            }

            var messages = _messages.Skip(Math.Max(0, _messages.Count() - messageNum));
            return messages.ToArray();
        }

        public Server CreateNewServer()
        {
            try
            {
                NamedPipeServerStream serverStream = new NamedPipeServerStream("PipesOfPiece", PipeDirection.InOut, _serverThreads, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                Console.WriteLine("Creating new server");
                var server = new Server(serverStream, this);
                server.NewMessageReceived += OnServerOnNewMessageReceived;
                _servers.TryAdd(server);
                return server;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("Unable to create new server " + e);
                _keepCreatingNewServer = true;
                Console.ForegroundColor = ConsoleColor.Green;
            }
            return null;
        }

        private void OnServerOnNewMessageReceived(object s, Server.NewMessageEventArgs e)
        {
            _messages.Add(e.Message);
            if (_messages.Count() > _maxMessages) _messages = _messages.Skip(Math.Max(0, _messages.Count() - _maxMessages)).ToList();
            foreach (var srv in _servers)
            {
                if ((srv.ConnectedUserName != e.User) && (srv.ServerThread != null))
                {
                    srv.SendMessage(e.Message);
                }
            }
        }
    }
}
