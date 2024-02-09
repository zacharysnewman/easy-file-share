using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace EasyFileShare
{
    class Program
    {
        private const int FileTransferPort = 9001;

        public static string? destinationIpAddress = null;
        public static string? filePathForFileToSend = null;
        public static string myPublicIp = "";

        static async Task Main(string[] args)
        {
            await Utils.ForwardPort(FileTransferPort);

            myPublicIp = await Utils.GetPublicIpAddress();

            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            while (true)
            {
                DisplayMenu();

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        SetDestinationIpOption();
                        break;
                    case "2":
                        SetFilePathToSendOption();
                        break;
                    case "3":
                        if (destinationIpAddress != null && filePathForFileToSend != null)
                        {
                            await SendFileOption();
                        }
                        else
                        {
                            Console.WriteLine(">>>> WARNING: You must set the ip address and file path first!");
                        }
                        break;
                    case "4":
                        await ReceiveFileOption(downloadsFolder);
                        break;
                    case "5":
                        Console.WriteLine("End of line.");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please enter an option 1-5.");
                        break;
                }
            }
        }

        static void DisplayMenu()
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine($"- My Public IP Address: {Program.myPublicIp}");
            Console.WriteLine($"- Remote IP Address: {Program.destinationIpAddress ?? "Not Set"}");
            Console.WriteLine($"- File Path To Send: {Program.filePathForFileToSend ?? "Not Set"}");
            Console.WriteLine("Options:");
            Console.WriteLine("1. Set destination ip address");
            Console.WriteLine("2. Set file path for file to send");
            Console.WriteLine("3. Send file");
            Console.WriteLine("4. Receive file");
            Console.WriteLine("5. Exit");
            Console.Write("Enter your choice: ");
        }

        static async Task SendFileOption()
        {
            await Task.Run(() => StartPeer(Program.destinationIpAddress, FileTransferPort, Program.filePathForFileToSend));
        }

        static async Task ReceiveFileOption(string downloadsFolder)
        {
            var server = new TcpListener(IPAddress.Any, FileTransferPort);
            server.Start();
            Console.WriteLine($"Listening for incoming files on port {FileTransferPort}. Press Enter to exit receive mode.");
            await Task.Run(() => ReceiveFiles(downloadsFolder, server));
            server.Stop();
        }

        static void SetDestinationIpOption()
        {
            string ipAddressInput = "";
            while (!IPAddress.TryParse(ipAddressInput, out IPAddress? _))
            {
                Console.Write("Enter the peer's IP address: ");
                ipAddressInput = Console.ReadLine() ?? "";
            }
            Program.destinationIpAddress = ipAddressInput;
        }

        static void SetFilePathToSendOption()
        {
            string filePathInput = "";
            while (!File.Exists(filePathInput))
            {
                Console.Write("Enter the file path to send: ");
                filePathInput = Console.ReadLine() ?? "";
            }
            Program.filePathForFileToSend = filePathInput;
        }

        static async Task ReceiveFiles(string defaultReceiveFolder, TcpListener server)
        {
            try
            {
                var hasReceivedFiles = false;
                while (!hasReceivedFiles)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    hasReceivedFiles = await HandlePeerAsync(client, defaultReceiveFolder);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static async Task StartPeer(string ipAddress, int port, string sendFilePath)
        {
            try
            {
                using (TcpClient client = new TcpClient(ipAddress, port))
                using (NetworkStream stream = client.GetStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(sendFilePath);
                    using (var fileStream = new FileStream(sendFilePath, FileMode.Open))
                    {
                        writer.Write(fileStream.Length);
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await stream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            int progressPercentage = (int)((double)totalBytesRead / fileStream.Length * 100);
                            Console.WriteLine($"Progress: {progressPercentage}%");
                        }
                    }

                    Console.WriteLine("File sent successfully.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static async Task<bool> HandlePeerAsync(TcpClient client, string defaultReceiveFolder)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string fileName = Path.GetFileName(reader.ReadString());
                    long fileSize = reader.ReadInt64();

                    Console.WriteLine($"Receiving file: {fileName}");
                    string filePath = Path.Combine(defaultReceiveFolder, fileName);
                    Console.WriteLine($"Path to write: {filePath}");

                    using (FileStream fileStream = File.Create(filePath))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesRead = 0;

                        while (totalBytesRead < fileSize && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            int progressPercentage = (int)((double)totalBytesRead / fileSize * 100);
                            Console.WriteLine($"Progress: {progressPercentage}%");
                        }
                    }

                    Console.WriteLine("File received successfully.");
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error handling peer: " + e.Message);
                return false;
            }
        }
    }
}