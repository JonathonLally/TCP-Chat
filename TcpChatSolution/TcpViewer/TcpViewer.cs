using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpViewer
{
    class TcpViewer
    {
        //Vars
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }

        private bool _disconnectedRequested = false;
        public readonly int BufferSize = 2 * 1024;
        private NetworkStream msgStream = null;

        //Constructor
        public TcpViewer(string address, int port)
        {
            ServerAddress = address;
            Port = port;
            _client = new TcpClient();
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;
        }

        public void Connect()
        {
            _client.Connect(ServerAddress, Port);
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            if (_client.Connected)
            {
                Console.WriteLine("Connected to server at {0} as viewer", endPoint);
                msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes("Viewer");
                msgStream.Write(msgBuffer, 0, msgBuffer.Length);

                if (!IsDisconnected(_client))
                {
                    Running = true;
                }
                else
                {
                    Console.WriteLine("Cannot connect");
                    CleanupNetworkResources();

                }
            } else
            {
                CleanupNetworkResources();
                Console.WriteLine("Cannot connect");
            }

        }

        public void Disconnect()
        {
            Running = false;
            _disconnectedRequested = true;
            Console.WriteLine("Disconnecting");
        }

        public void ListenForMessages()
        {
            bool wasRunning = Running;

            while (Running)
            {
                int messageLength = _client.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    msgStream.Read(msgBuffer, 0, messageLength);
                    String msg = Encoding.UTF8.GetString(msgBuffer);
                    Console.WriteLine(msg);
                }

                if (IsDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server Disconnected");
                }

                Running &= !_disconnectedRequested;
            }

            CleanupNetworkResources();
        }

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

        private void CleanupNetworkResources()
        {
            msgStream?.Close();
            msgStream = null;
            _client.Close();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Viewing Tcp Chat");
            TcpViewer viewer = new TcpViewer("localhost", 6000);
            viewer.Connect();
            viewer.ListenForMessages();
        }
    }
}
