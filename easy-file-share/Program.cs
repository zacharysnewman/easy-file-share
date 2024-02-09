using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace easyfileshare
{
    class MainClass
    {
        private static TcpListener? server;

        public static async Task Main(string[] args)
        {
            await Utils.ForwardPort(9001);

            var publicIp = await Utils.GetPublicIpAddress();
            Console.WriteLine($"Public IP Address: {publicIp}");

            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            while (true)
            {
                Console.WriteLine("Options:");
                Console.WriteLine("1. Send file");
                Console.WriteLine("2. Receive file");
                Console.WriteLine("3. Exit");

                Console.Write("Enter your choice (1/2/3): ");
                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        Console.Write("Enter the peer's IP address: ");
                        string peerIpAddress = Console.ReadLine() ?? "";
                        Console.Write("Enter the file path to send: ");
                        string sendFilePath = Console.ReadLine() ?? "";
                        Task.Run(() => StartPeer(peerIpAddress, 9001, sendFilePath)).Wait();
                        break;
                    case "2":
                        server = new TcpListener(IPAddress.Any, 9001);
                        server.Start();
                        Console.WriteLine($"Listening for incoming files on port 9001. Press Enter to exit receive mode.");
                        Task.Run(() => ReceiveFiles(downloadsFolder)).Wait();
                        server.Stop();
                        break;
                    case "3":
                        Console.WriteLine("Exiting the program.");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                        break;
                }
            }
        }

        static void StartPeer(string ipAddress, int port, string sendFilePath)
        {
            try
            {
                TcpClient client = new TcpClient(ipAddress, port);
                NetworkStream stream = client.GetStream();
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write(sendFilePath);
                using (var fileStream = new FileStream(sendFilePath, FileMode.Open))
                {
                    writer.Write(fileStream.Length);
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    long totalBytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        int progressPercentage = (int)((double)totalBytesRead / fileStream.Length * 100);
                        Console.WriteLine($"Progress: {progressPercentage}%");
                    }
                }

                Console.WriteLine("File sent successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static async Task ReceiveFiles(string defaultReceiveFolder)
        {
            try
            {
                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    HandlePeer(client, defaultReceiveFolder);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static void HandlePeer(TcpClient client, string defaultReceiveFolder)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                string filePath = reader.ReadString();
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    Console.WriteLine($"Sending file to: {filePath}");
                    string fileName = Path.GetFileName(filePath);
                    long fileSize = new FileInfo(filePath).Length;
                    writer.Write(fileName);
                    writer.Write(fileSize);
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            int progressPercentage = (int)((double)totalBytesRead / fileSize * 100);
                            Console.WriteLine($"Progress: {progressPercentage}%");
                        }
                    }

                    Console.WriteLine("File transfer complete.");
                }
                else
                {
                    Console.WriteLine($"Receiving files to: {defaultReceiveFolder}");
                    Directory.CreateDirectory(defaultReceiveFolder);
                    string fileName = reader.ReadString();
                    long fileSize = reader.ReadInt64();
                    using (FileStream fileStream = File.Create(Path.Combine(defaultReceiveFolder, fileName)))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        while (totalBytesRead < fileSize && (bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            int progressPercentage = (int)((double)totalBytesRead / fileSize * 100);
                            Console.WriteLine($"Progress: {progressPercentage}%");
                        }
                    }

                    Console.WriteLine("File received successfully.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error handling peer: " + e.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
