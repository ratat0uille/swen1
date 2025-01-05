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
using MTCG.Models;

namespace MTCG.Server
{
    internal class Server
    {
        /*----------------------------------START-ASYNC-------------------------------------*/
        public static async Task StartAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 10001);
            listener.Start();
            Console.WriteLine("Server started on port 10001...");

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

        /*--------------------------------HANDLE-CLIENT-ASYNC---------------------------------------*/
        private static async Task HandleClientAsync(TcpClient client)
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
                    

                    switch (routeResult)
                    {
                        case "NotFound":
                            WriteResponse(NotFound(), writer);
                            break;
                        case "BadRequest":
                            WriteResponse(BadRequest(), writer);
                            break;
                        case "Login":
                            WriteResponse(Login(request, stream), writer);
                            break;
                        case "Register":
                            WriteResponse(Register(request, stream), writer);
                            break;
                        default:
                            WriteResponse(NotFound(), writer);
                            break;
                    }

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error handling client: {exception.Message}");
            }
        }


        /*--------------------------------GENERATE-RESPONSE---------------------------------------*/
        static string GenerateResponse(string status, string content)
        {
            return $"HTTP/1.1 {status}\r\n" +
                   "Content-Type: text/plain\r\n" +
                   $"Content-Length: {Encoding.UTF8.GetByteCount(content)}\r\n" +
                   "Connection: close\r\n" +
                   "\r\n" +
                   $"{content}";
        }

        /*----------------------------------WRITE-RESPONSE-------------------------------------*/
        static async void WriteResponse(string response, StreamWriter writer)
        {
            Console.WriteLine(response);
            await writer.WriteAsync(response);
            await writer.FlushAsync();
        }

        /*----------------------------------LOGIN-METHOD-------------------------------------*/
        static string Login(HttpRequest request, Stream stream)
        {
            // 200 OK
            // 401 Unauthorized
            string response;
            var (username, password) = Parser.BodyParser(stream, request);

            string filePath = "../UserData/users.json";

            if (!File.Exists(filePath))
            {
                return GenerateResponse("404", "Not Found");

            }

            var users = JsonConvert.DeserializeObject<List<Users>>(File.ReadAllText(filePath)) ?? new List<Users>();

            var user = users.Find(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                return GenerateResponse("200", "OK");
            }
            else
            {
                return GenerateResponse("401", "Invalid username or password");

            }

        }

        /*----------------------------------REGISTER-METHOD-------------------------------------*/
        static string Register(HttpRequest request, Stream stream)
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

                return GenerateResponse("409", "User already exists");
            }

            users.Add(new Users{Username = username, Password = password});
            
            File.WriteAllText(filePath, JsonConvert.SerializeObject(users, Formatting.Indented));
            
            return GenerateResponse("201", "User created");
            
        }

        /*----------------------------------400-BAD-REQUEST-METHOD-------------------------------------*/
        static string BadRequest()
        {
            return GenerateResponse("400", "Bad Request");
        }

        /*----------------------------------404-NOT-FOUND-METHOD-------------------------------------*/
        static string NotFound()
        {
            return GenerateResponse("404", "Not Found");
        }

    }
}
