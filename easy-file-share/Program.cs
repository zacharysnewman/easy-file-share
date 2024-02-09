using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace easyfileshare
{
    class MainClass
    {
        public static async Task Main(string[] args)
        {
            //var publicIp = await Utils.GetPublicIpAddress();
            //await Utils.ForwardPort(9001);

            //Console.WriteLine($"Hello {publicIp}!");
            // 174.164.184.12
            Task.Run(_Server_Main);
            _Client_Main();
        }

        static void _Client_Main()
        {
            try
            {
                Console.Write("Enter the server's IP address: ");
                string serverIp = Console.ReadLine();

                if (IPAddress.TryParse(serverIp, out IPAddress ipAddress))
                {
                    TcpClient client = new TcpClient(ipAddress.ToString(), 9001);
                    NetworkStream stream = client.GetStream();
                    BinaryWriter writer = new BinaryWriter(stream);

                    // Send file path to server
                    Console.Write("Enter the file path: ");
                    string filePath = Console.ReadLine();
                    writer.Write(filePath);

                    // Receive file size from server
                    BinaryReader reader = new BinaryReader(stream);
                    long fileSize = reader.ReadInt64();
                    Console.WriteLine($"Receiving file of size {fileSize} bytes.");

                    // Receive file content from server
                    using (var fileStream = new FileStream("received_file.txt", FileMode.Create))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while (fileSize > 0 && (bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            fileSize -= bytesRead;
                        }
                    }

                    Console.WriteLine("File received successfully.");
                }
                else
                {
                    Console.WriteLine("Invalid IP address format. Please enter a valid IP address.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }


        static async Task _Server_Main()
        {
            try
            {
                string publicIp = await Utils.GetPublicIpAddress();
                Console.WriteLine($"Public IP Address: {publicIp}");

                int port = 9001;

                // Forward the port using Open.Nat
                await Utils.ForwardPort(port);

                TcpListener server = new TcpListener(IPAddress.Any, port);

                // Start listening for client requests
                server.Start();
                Console.WriteLine($"Server listening on port {port}");

                while (true)
                {
                    // Accept the client connection
                    TcpClient client = await server.AcceptTcpClientAsync();

                    // Handle client in a separate thread or method
                    await _Server_HandleClient(client);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static async Task _Server_HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                BinaryReader reader = new BinaryReader(stream);

                // Receive file path from client
                string filePath = reader.ReadString();
                Console.WriteLine($"Receiving file: {filePath}");

                // Implement file transfer logic (replace with your own implementation)
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    // Send file size to client
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(fileStream.Length);

                    // Send file content to client
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                    }
                }

                Console.WriteLine("File transfer complete.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error handling client: " + e.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
}