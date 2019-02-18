using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatApp
{
    class TcpServer
    {
        //Variables
        //TCP Listener
        private TcpListener listener;

        //Types of Clients
        private List<TcpClient> viewerList = new List<TcpClient>();
        private List<TcpClient> messengerList = new List<TcpClient>();

        //Messenger Name Dictionary
        private Dictionary<TcpClient, string> namesDictionary = new Dictionary<TcpClient, string>();

        //Message Queue
        private Queue<string> messageQueue = new Queue<string>();

        //Other 
        public readonly string ChatName;
        public readonly int Port;
        public bool Running { get; private set; }

        //Buffer
        public readonly int BufferSize = 2 * 1024;

        //Methods
        //Constructor
        public TcpServer(string chatName, int port)
        {
            ChatName = chatName;
            Port = port;
            Running = false;

            listener = new TcpListener(IPAddress.Any, Port);
        }

        //Shutdown Server
        public void Shutdown()
        {
            Running = false;
            Console.WriteLine("Shutting Down Server");
        }

        //Start Server
        public void Run()
        {
            Console.WriteLine("Starting the \"{0}\" TCP Chat Server on port {1}.", ChatName, Port);
            listener.Start();
            Running = true;

            //Server Loop
            while (Running)
            {
                if (listener.Pending())
                    handleNewConnection();

                CheckForDisconnects();
                CheckForNewMessages();
                SendMessages();

                //Thread.Sleep(10);
            }

            //After Server Loop
            foreach (TcpClient v in viewerList)
                CleanUpClient(v);

            foreach (TcpClient m in messengerList)
                CleanUpClient(m);

            listener.Stop();
            Console.WriteLine("Server shutting down");
        }

        //New Connection
        private void handleNewConnection()
        {
            bool valid = false;
            TcpClient newClient = listener.AcceptTcpClient();
            NetworkStream netStream = newClient.GetStream();

            newClient.SendBufferSize = BufferSize;
            newClient.ReceiveBufferSize = BufferSize;

            EndPoint endpoint = newClient.Client.RemoteEndPoint;
            Console.WriteLine("New Client From : ", endpoint);

            //Identify
            byte[] msgBuffer = new byte[BufferSize];
            int bytesRead = netStream.Read(msgBuffer, 0, msgBuffer.Length);

            if (bytesRead > 0)
            {
                string message = Encoding.UTF8.GetString(msgBuffer, 0, bytesRead);

                if (message == "viewer")
                {
                    valid = true;
                    viewerList.Add(newClient);

                    Console.WriteLine("Viewer Added");

                    message = String.Format("Welcome to Server", ChatName);
                    msgBuffer = Encoding.UTF8.GetBytes(message);
                    netStream.Write(msgBuffer, 0, msgBuffer.Length);
                }
                else if (message.StartsWith("name:"))
                {
                    //Messenger
                    String name = message.Substring(message.IndexOf(':') + 1);

                    if ((name != string.Empty) && (!namesDictionary.ContainsValue(name)))
                    {
                        valid = true;
                        namesDictionary.Add(newClient, name);
                        messengerList.Add(newClient);

                        Console.WriteLine("{0} is a Messenger with the name {1}.", endpoint, name);
                        messageQueue.Enqueue(String.Format("{0} has joined the chat.", name));
                    }
                }
                else
                {
                    Console.WriteLine("Not a valid viewer or Messenger");
                    CleanUpClient(newClient);
                }
            }

            if (!valid)
                newClient.Close();

        }

        //Check if clients leave
        private void CheckForDisconnects()
        {
            foreach (TcpClient v in viewerList.ToArray())
            {
                if (IsDisconnected(v))
                {
                    Console.WriteLine("Viewer {0} has left.", v.Client.RemoteEndPoint);
                    viewerList.Remove(v);
                    CleanUpClient(v);
                }
            }

            foreach (TcpClient m in messengerList.ToArray())
            {
                if (IsDisconnected(m))
                {
                    string name = namesDictionary[m];

                    Console.WriteLine(" {0} has left.", name);
                    messageQueue.Enqueue(String.Format("{0} has left the chat", name));

                    messengerList.Remove(m);
                    namesDictionary.Remove(m);
                    CleanUpClient(m);
                }
            }
        }

        //Check for Messages, put in queue
        private void CheckForNewMessages()
        {
            foreach (TcpClient m in messengerList)
            {
                int messageLength = m.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    m.GetStream().Read(msgBuffer, 0, msgBuffer.Length);

                    string msg = String.Format("{0}: {1}", namesDictionary[m], Encoding.UTF8.GetString(msgBuffer));
                    messageQueue.Enqueue(msg);
                }
            }
        }

        //Clear Message Queue and send
        private void SendMessages()
        {
            foreach (string msg in messageQueue)
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);

                foreach (TcpClient v in viewerList)
                    v.GetStream().Write(msgBuffer, 0, msgBuffer.Length);
            }
            messageQueue.Clear();
        }

        //Check if Socket has Disconnected
        private static bool IsDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException se)
            {
                return true;
            }
        }

        //Cleanup Resources
        private static void CleanUpClient(TcpClient client)
        {
            client.GetStream().Close();
            client.Close();
        }

        public static TcpServer chat;

        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            chat.Shutdown();
            args.Cancel = true;
        }

        public static void Main(string[] args)
        {
            string name = "C# Chat";
            int port = 6000;
            chat = new TcpServer(name, port);

            Console.CancelKeyPress += InterruptHandler;
            chat.Run();
        }
    }
}
