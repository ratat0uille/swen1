
namespace MTCG.Routing
{
    public class Router
    {
        public string Route(HttpRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Method) || string.IsNullOrEmpty(request.Path))
            {
                return "BadRequest";
            }

            string method = request.Method;
            string path = request.Path;

            return path switch
            {
                "/" => "Homepage",
                "/login" when method == "GET" => "GetLogin",
                "/register" when method == "GET" => "GetRegister",
                "/register" when method == "POST" => "PostRegister",
                "/login" when method == "POST" => "PostLogin",
                _ => "NotFound"
            };
        }
    }
}