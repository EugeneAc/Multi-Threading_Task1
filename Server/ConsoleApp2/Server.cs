
namespace ConsoleApp2
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
            this._server = server;
            this._serverManager = manager;
            this._writer = new StreamWriter(server);
            this._reader = new StreamReader(server);
            Console.WriteLine("Waiting for connetion...  ");
            this._server.BeginWaitForConnection(new AsyncCallback(this.ConnectionCallback), _server);
        }

        private void LinteningStream()
        {
            try
            {
                while (true)
                {
                    var line = this._reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        this.NewMessageReceived?.Invoke(this, new NewMessageEventArgs { Message = line, User = this.ConnectedUserName});
                        Console.WriteLine(line);
                    }
                    else
                    {
                        this._reader.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" Pipe error " + e.Message);
            }
        }

        private void KillServer()
        {
            this.SendMessage("Server Over");
        }

        public async void SendMessage(string message)
        {
            try
            {
                await this._writer.WriteLineAsync(message);
                this._writer.Flush();
                this._server.WaitForPipeDrain();

                if (message == "Server Over")
                {
                    this._server.Close();
                    this._server.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(this.ConnectedUserName + " Disconnected");
                this._server.Close();
                this._server.Dispose();
            }
        }

        private void ConnectionCallback(IAsyncResult ar)
        {
            this._server.EndWaitForConnection(ar);
            this._serverManager.CreateNewServer();
            this.ServerThread = Thread.CurrentThread;
            this._writer.WriteLine("Welcome to server on thread " + this.ServerThread.ManagedThreadId);
            this._writer.Flush();
            this._server.WaitForPipeDrain();

            Console.WriteLine("Waiting User Name");
            this.ConnectedUserName = this._reader.ReadLine();
            Console.WriteLine(this.ConnectedUserName + " connected");

            Task.Factory.StartNew(this.LinteningStream);
            this.SendHistory();
            while (true)
            {
                // keep thread alive;
            }
        }

        private void SendHistory()
        {
           foreach (var t in this._serverManager.GetMessageHistory(10))
                {
                    this.SendMessage(t);
                }
           this.SendMessage("!"); // End Message History
        }
    }
}
