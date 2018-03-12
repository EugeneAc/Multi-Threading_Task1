
namespace Client
{
    using System;
    using System.Threading.Tasks;
    using System.IO;
    using System.IO.Pipes;
    using System.Security.Principal;
    using System.Threading;

    public class Client
    {
        private readonly Random _rnd = new Random();

        private StreamReader _reader;

        private StreamWriter _writer;

        private NamedPipeClientStream _client;

        private string[] clientmessages = new string[] { "hello", "hello 1", "bye", "Luke I am your father", "Star wars", "phone", "random message", "you're dead", "monitor", "mouse" };

        private string _userName;

        public Client(string userName)
        {
            Task.Factory.StartNew(StartClient,userName);
            
        }

        private void StartClient(object userName)
        {
            _client = new NamedPipeClientStream(
                ".",
                "PipesOfPiece",
                PipeDirection.InOut,
                PipeOptions.Asynchronous,
                TokenImpersonationLevel.Delegation);
            Console.WriteLine("Connecting to server...\n");
            _client.Connect();
            _reader = new StreamReader(_client);
            _writer = new StreamWriter(_client);

            Console.WriteLine(_reader.ReadLine());

            _userName = (string)userName;
             _writer.WriteLine(_userName);
            _writer.Flush();
            Console.WriteLine("Connected with name " + _userName);
            _client.WaitForPipeDrain();
            ReadHistory();

            Task.Factory.StartNew(StartListening);
            StartSendingMessages();
            Console.WriteLine("Type new message OR Press Enter to Finish");
            var message = Console.ReadLine();
            while (!string.IsNullOrEmpty(message))
            { 
                SendMessage(message);
                message = Console.ReadLine();
            }
        }

        private void StartSendingMessages()
        {
            // sending random messages
            Console.WriteLine(string.Empty);
            Thread.Sleep(500);
                for (int i = 0; i < _rnd.Next(30, 50); i++)
                {
                    Thread.Sleep(_rnd.Next(100, 2000));
                    SendMessage(clientmessages[_rnd.Next(10)]);
                }
        }

        private void SendMessage(string message)
        {
            string phrase = _userName + " says: " + message;
            try
            {
                Console.WriteLine(phrase);
                _writer.WriteLine(phrase);
                _writer.Flush();
                _client.WaitForPipeDrain();
            }
            catch (Exception e)
            {
                Console.WriteLine("Sending error :" + e.Message);
            }
            
        }

        private void ReadHistory()
        {
            Console.WriteLine("Message history");
            Console.ForegroundColor = ConsoleColor.Red;
            bool readHistory = true;
            while (readHistory)
            {
                var readLine = _reader.ReadLine();
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
            Console.WriteLine(string.Empty);
        }

        private void StartListening()
        {
            bool continueRead = true;
            try
            {
                while (continueRead)
                {
                    var line = _reader.ReadLine();

                    if (line == "Server Over")
                    {
                        continueRead = false;
                    }

                    if (!string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine(line);
                    }
                    else
                    {
                        _reader.Close(); // close stream when "Server Over"
                        _client.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Server error :" + e.Message);
                _client.Close();
                _client.Dispose(); // close stream when server error
            }
            Console.WriteLine("Reading Over");
        }
    }
}
