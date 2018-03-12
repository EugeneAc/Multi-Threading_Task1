
namespace Server
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading;
    using System.Threading.Tasks;

    public class Server
    {

        private readonly NamedPipeServerStream _server;

        private readonly StreamWriter _writer;

        private readonly StreamReader _reader;

        private readonly ServerManager _serverManager;

        public string ConnectedUserName { get; private set; }

        public Thread ServerThread { get; private set; }

        public class NewMessageEventArgs : EventArgs
        {
            public string Message { get; internal set; }

            public string User { get; internal set; }
        }

        public event EventHandler<NewMessageEventArgs> NewMessageReceived;

        public Server(NamedPipeServerStream server, ServerManager manager)
        {
            _server = server;
            _serverManager = manager;
            _writer = new StreamWriter(server);
            _reader = new StreamReader(server);
            Console.WriteLine("Waiting for connetion...  ");
            _server.BeginWaitForConnection(new AsyncCallback(ConnectionCallback), _server);
        }

        private void LinteningStream()
        {
            try
            {
                while (true)
                {
                    var line = _reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        NewMessageReceived?.Invoke(this, new NewMessageEventArgs { Message = line, User = ConnectedUserName});
                        Console.WriteLine(line);
                    }
                    else
                    {
                        _reader.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" Pipe error " + e.Message);
                _reader.Close();
            }
        }

        private void KillServer()
        {
            SendMessage("Server Over");
        }

        public async void SendMessage(string message)
        {
            try
            {
                await _writer.WriteLineAsync(message);
                _writer.Flush();
                _server.WaitForPipeDrain();

                if (message == "Server Over")
                {
                    _server.Close();
                    _server.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(ConnectedUserName + " Disconnected");
                _server.Close();
                _server.Dispose();
            }
        }

        private void ConnectionCallback(IAsyncResult ar)
        {
            _server.EndWaitForConnection(ar);
            _serverManager.CreateNewServer();
            ServerThread = Thread.CurrentThread;
            _writer.WriteLine("Welcome to server on thread " + ServerThread.ManagedThreadId);
            _writer.Flush();
            _server.WaitForPipeDrain();

            Console.WriteLine("Waiting User Name");
            ConnectedUserName = _reader.ReadLine();
            Console.WriteLine(ConnectedUserName + " connected");

            Task.Factory.StartNew(LinteningStream);
            SendHistory();
            while (true)
            {
                // keep thread alive;
            }
        }

        private void SendHistory()
        {
           foreach (var t in _serverManager.GetMessageHistory(10))
                {
                    SendMessage(t);
                }
           SendMessage("!"); // End Message History
        }
    }
}
