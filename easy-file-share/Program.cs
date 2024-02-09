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
            await Utils.ForwardPort(9001);

            var publicIp = await Utils.GetPublicIpAddress();
            Console.WriteLine($"Public IP Address: {publicIp}");

            // Use the user's Downloads folder as the default receive directory
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Task.Run(() => _StartPeer("127.0.0.1", 9001, downloadsFolder));

            // Main loop
            while (true)
            {
                Console.WriteLine("Options:");
                Console.WriteLine("1. Send file");
                Console.WriteLine("2. Receive file");
                Console.WriteLine("3. Exit");

                Console.Write("Enter your choice (1/2/3): ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        // Send file
                        Console.Write("Enter the peer's IP address: ");
                        string peerIpAddress = Console.ReadLine();
                        Console.Write("Enter the file path to send: ");
                        string sendFilePath = Console.ReadLine();
                        Task.Run(() => _StartPeer(peerIpAddress, 9001, sendFilePath)).Wait();
                        break;
                    case "2":
                        // Receive file
                        Console.WriteLine($"Receiving files to: {downloadsFolder}");
                        Console.WriteLine("Press Enter to exit receive mode.");
                        Console.ReadLine(); // Wait for Enter key
                        break;
                    case "3":
                        // Exit the loop and finish execution
                        Console.WriteLine("Exiting the program.");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                        break;
                }
            }
        }

        static async Task _StartPeer(string ipAddress, int port, string defaultReceiveFolder)
        {
            try
            {
                TcpListener server = new TcpListener(IPAddress.Any, port);

                // Start listening for client requests
                server.Start();
                Console.WriteLine($"Peer listening on port {port}");

                while (true)
                {
                    // Accept the client connection
                    TcpClient client = await server.AcceptTcpClientAsync();

                    // Handle client in a separate thread or method
                    await _HandlePeer(client, defaultReceiveFolder);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static async Task _HandlePeer(TcpClient client, string defaultReceiveFolder)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                // Check if the user provided a file path
                string filePath = reader.ReadString();
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    // Send file to peer
                    Console.WriteLine($"Sending file: {filePath}");

                    // Get file name and size
                    string fileName = Path.GetFileName(filePath);
                    long fileSize = new FileInfo(filePath).Length;

                    // Send file name and size to peer
                    writer.Write(fileName);
                    writer.Write(fileSize);

                    // Implement file transfer logic (replace with your own implementation)
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        // Send file content to peer with progress percentage
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            // Calculate and display progress percentage
                            int progressPercentage = (int)((double)totalBytesRead / fileSize * 100);
                            Console.WriteLine($"Progress: {progressPercentage}%");
                        }
                    }

                    Console.WriteLine("File transfer complete.");
                }
                else
                {
                    // Receive file from peer
                    Console.WriteLine($"Receiving files to: {defaultReceiveFolder}");

                    // Create the downloads folder if it doesn't exist
                    Directory.CreateDirectory(defaultReceiveFolder);

                    // Get file name and size from peer
                    string fileName = reader.ReadString();
                    long fileSize = reader.ReadInt64();

                    // Receive file content from the peer with progress percentage
                    using (FileStream fileStream = File.Create(Path.Combine(defaultReceiveFolder, fileName)))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        while (totalBytesRead < fileSize && (bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            // Calculate and display progress percentage
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
