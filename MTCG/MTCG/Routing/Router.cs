namespace MTCG.Routing
{
    public class Router
    {
        public string Route(string request)
        {
            if (request.StartsWith("GET /login"))
            {
                return "HTTP/1.1 200 OK\r\n" +
                       "Content-Type: text/plain\r\n" +
                       "Content-Length: 5\r\n" +
                       "\r\n" +
                       "Login";
            }else if (request.StartsWith("GET /register"))
            {
                return "HTTP/1.1 200 OK\r\n" +
                       "Content-Type: text/plain\r\n" +
                       "Content-Length: 8\r\n" +
                       "\r\n" +
                       "Register";
            }
            else
            {
                return "HTTP/1.1 404 Not Found\r\n" +
                       "Content-Type: text/plain\r\n" +
                       "Content-Length: 9\r\n" +
                       "\r\n" +
                       "Login";
            }
        }
    }
}
