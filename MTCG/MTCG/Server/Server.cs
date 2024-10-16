﻿using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using MTCG.Routing;

namespace MTCG.Server
{
    internal class Server
    {
        static async Task Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 8080);
            listener.Start();
            Console.WriteLine("Server started on port 8080...");

            try
            {
                while (true)
                {
                    if (listener.Pending())
                    {
                        TcpClient client = await listener.AcceptTcpClientAsync();
                        _ = HandleClientAsync(client);

                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
            finally
            {
                listener.Stop();
                Console.WriteLine("Server stopped.");
            }

            async Task HandleClientAsync(TcpClient client)
            {
                try
                {
                    using (client)
                    await using (NetworkStream stream = client.GetStream())
                    {
                        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                        await using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                        string request = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(request))
                        {
                            return;
                        }

                        string line;
                        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                        {
                            //process headers?
                        }

                        Console.WriteLine("Received request:");
                        Console.WriteLine(request);

                        Router router = new Router();
                        string response = router.Route(request);

                        await writer.WriteAsync(response);


                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error handling client: {exception.Message}");
                }
            }
        }
    }
}
