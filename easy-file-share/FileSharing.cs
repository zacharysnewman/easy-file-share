using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace EasyFileShare
{
    public class FileSharing
    {
        public static async Task ReceiveFiles(string defaultReceiveFolder, TcpListener server)
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

        public static async Task StartPeer(string ipAddress, int port, string sendFilePath)
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