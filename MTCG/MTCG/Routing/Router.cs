namespace MTCG.Routing
{
    public class Router
    {
        public string Route(string request)
        {
            string[] requestParts = request.Split(' ');
            if (requestParts.Length > 2)
            {
                return "HTTP/1.1 400 Bad Request\r\n\r\n";
            }

            string method = requestParts[0];
            string path = requestParts[1];

            if (method == "GET" && path == "/login")
            {
                return "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 5\r\n\r\nLogin";
            }
            else if (method == "GET" && path == "/register")
            {
                return "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 8\r\n\r\nRegister";
            }
            else if (method == "POST" && path == "/login")
            {
                //muss noch login logic und so implementieren
                return "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 9\r\n\r\nLogged In";
            }
            else if (method == "POST" && path == "/register")
            {
                //und registration logic
                return "HTTP/1.1 201 Created\r\nContent-Type: text/plain\r\nContent-Length: 10\r\n\r\nRegistered";
            }
            else
            {
                return "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\nContent-Length: 9\r\n\r\nNot Found";
            }

        }
    }
}
