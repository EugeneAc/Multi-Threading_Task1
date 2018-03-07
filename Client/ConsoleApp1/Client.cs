using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
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
            Task.Factory.StartNew(this.StartClient,userName);
            
        }

        private void StartClient(object userName)
        {
            this._client = new NamedPipeClientStream(
                ".",
                "PipesOfPiece",
                PipeDirection.InOut,
                PipeOptions.Asynchronous,
                TokenImpersonationLevel.Delegation);
            Console.WriteLine("Connecting to server...\n");
            this._client.Connect();
            this._reader = new StreamReader(this._client);
            this._writer = new StreamWriter(this._client);

            Console.WriteLine(this._reader.ReadLine());

            this._userName = (string)userName;
            this._writer.WriteLine(this._userName);
            this._writer.Flush();
            Console.WriteLine("Connected with name " + this._userName);
            this._client.WaitForPipeDrain();
            this.ReadHistory();

            Task.Factory.StartNew(this.StartListening);
            this.StartSendingMessages();
            Console.WriteLine("Type new message OR Press Enter to Finish");
            var message = Console.ReadLine();
            while (!string.IsNullOrEmpty(message))
            { 
                this.SendMessage(message);
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
                    this.SendMessage(this.clientmessages[this._rnd.Next(10)]);
                }
        }

        private void SendMessage(string message)
        {
            string phrase = this._userName + " says: " + message;
            try
            {
                Console.WriteLine(phrase);
                this._writer.WriteLine(phrase);
                this._writer.Flush();
                this._client.WaitForPipeDrain();
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
                var readLine = this._reader.ReadLine();
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
                    var line = this._reader.ReadLine();

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
                        this._reader.Close(); // close stream when "Server Over"
                        this._client.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Server error :" + e.Message);
                this._client.Close();
                this._client.Dispose(); // close stream when server error
            }
            Console.WriteLine("Reading Over");
        }
    }
}
