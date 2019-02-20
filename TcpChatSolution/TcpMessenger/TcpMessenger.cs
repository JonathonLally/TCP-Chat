using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpChatMessenger
{
    class TcpMessenger
    {
        //Vars
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }

        public readonly int BufferSize = 2 * 1024;
        //NetworkStream for messages
        private NetworkStream msgStream = null;
        public readonly string Name;

        //Constructor
        public TcpMessenger(string address, int port, string name)
        {
            ServerAddress = address;
            Port = port;
            Name = name;

            _client = new TcpClient();
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;
        }

        public void Connect()
        {
            //Try to Connect
            _client.Connect(ServerAddress, Port);
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            if (_client.Connected)
            {
                Console.WriteLine("Connected to server at {0}.", endPoint);
                msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes(String.Format("name:{0}", Name));
                msgStream.Write(msgBuffer, 0, msgBuffer.Length);

                if (!IsDisconnected(_client))
                {
                    Running = true;
                }
                else
                {
                    Console.WriteLine("Cannot connect, try a different name");
                    CleanupNetworkResources();
                }

            }

        }

        public void SendMessages()
        {
            bool wasRunning = Running;

            while (Running)
            {
                Console.Write("{0}> ", Name);
                string msg = Console.ReadLine();

                if ((msg.ToLower() == "quit") || (msg.ToLower() == "exit")) {
                    Console.WriteLine("Disconnecting");
                    Running = false;               
                }
                else if (msg != string.Empty)
                {
                    byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
                    msgStream.Write(msgBuffer, 0, msgBuffer.Length);
                }

                if (IsDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected");
                }
             }

            CleanupNetworkResources();
            if (wasRunning) Console.WriteLine("Disconnected");

        }

        private void CleanupNetworkResources()
        {
            msgStream?.Close();
            msgStream = null;
            _client.Close();

        }

        //Checks if socket disconnected
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

        public static void Main (string[] args)
        {
            Console.WriteLine("Please enter a username");
            string name = Console.ReadLine();

            string host = "localhost";
            int port = 6000;

            TcpMessenger messenger = new TcpMessenger(host, port, name);
            messenger.Connect();
            messenger.SendMessages();

        }
    }
}
