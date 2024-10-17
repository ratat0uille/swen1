using System;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MTCG.Routing;
using Newtonsoft.Json;
using HttpRequest = MTCG.Routing.HttpRequest;
using System.Collections.Generic;

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
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
                {
                    Routing.HttpRequest request = await Parser.ParseAsync(stream);

                    Router router = new Router();
                    string routeResult = router.Route(request);

                    if (routeResult == "Login")
                    {
                        Login(request, stream);
                    }else if (routeResult == "Register")
                    {
                        Register(request, stream);
                    }

                    string response = routeResult switch
                    {
                        "BadRequest" => GenerateResponse("400 Bad Request", "Bad Request"),
                        "NotFound" => GenerateResponse("404 Not Found", "Not Found")
                        //_ => GenerateResponse("200 OK", "Operation successful")
                    };
                    Console.WriteLine(response);
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
                   $"Content-Length: {Encoding.UTF8.GetByteCount(content)}\r\n" +
                   "Connection: close\r\n" +
                   "\r\n" +
                   $"{content}";
        }

        public class Users
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
        static void Login(HttpRequest request, Stream stream)
        {
            // 200 OK
            // 401 Unauthorized
            var (username, password) = Parser.BodyParser(stream, request);

            string filePath = "../UserData/users.json";

            if (!File.Exists(filePath))
            {
                GenerateResponse("404", "Not Found");
                return;
            }

            var users = JsonConvert.DeserializeObject<List<Users>>(File.ReadAllText(filePath)) ?? new List<Users>();

            var user = users.Find(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                GenerateResponse("200", "OK");
                return;
            }
            else
            {
                GenerateResponse("401", "Invalid username or password");
                return;
            }

        }

        static void Register(HttpRequest request, Stream stream)
        {
            // 201 created
            // 409 User already exists
            
            var (username, password) = Parser.BodyParser(stream, request);
            
            string filePath = "../UserData/users.json";
            
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]");
            }

            var users = JsonConvert.DeserializeObject<List<Users>>(File.ReadAllText(filePath)) ?? new List<Users>();

            if (users.Exists(user => user.Username == username))
            {

                GenerateResponse("409", "User already exists");
                return;
            }

            users.Add(new Users{Username = username, Password = password});
            
            File.WriteAllText(filePath, JsonConvert.SerializeObject(users, Formatting.Indented));
            
            GenerateResponse("201", "User created");

        }

    }
}
