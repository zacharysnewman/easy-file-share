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
            await Task.Run(() => FileSharing.StartPeer(Program.destinationIpAddress, FileTransferPort, Program.filePathForFileToSend));
        }

        static async Task ReceiveFileOption(string downloadsFolder)
        {
            var server = new TcpListener(IPAddress.Any, FileTransferPort);
            server.Start();
            Console.WriteLine($"Listening for incoming files on port {FileTransferPort}. Ctrl-C to exit without receiving files.");
            await Task.Run(() => FileSharing.ReceiveFiles(downloadsFolder, server));
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
    }
}