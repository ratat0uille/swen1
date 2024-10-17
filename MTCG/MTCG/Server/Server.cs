using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using MTCG.Routing;

namespace MTCG.Server
{
    internal class Server
    {
        public static async Task StartAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 8080);
            listener.Start();
            Console.WriteLine("Server started on port 8080...");

            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
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
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8){AutoFlush = true}) 
                {
                    string rawRequest = await reader.ReadToEndAsync();
                    Routing.HttpRequest request = Parser.Parse(rawRequest);

                    Router router = new Router();
                    string routeResult = router.Route(request);
                    string response = routeResult switch
                    {
                        "BadRequest" => GenerateResponse("400 Bad Request", "Bad Request"),
                        "NotFound" => GenerateResponse("404 Not Found", "Not Found"),
                        _ => GenerateResponse("200 OK", "Operation successful")
                    };
                    
                    await writer.WriteAsync(response);
                    await writer.FlushAsync();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error handling client: {exception.Message}");
            }
        }

        static string GenerateResponse(string status, string content)
        {
            return $"HTTP/1.1 {status}\r\n" +
                   "Content-Type: text/plain\r\n" +
                   $"Content-Length: {content.Length}\r\n" +
                   "Connection: close\r\n" +
                   "\r\n" +
                   $"{content}";
        }
    }
}