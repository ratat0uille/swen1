
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
                "/register" when method == "POST" => "Register",
                "/login" when method == "POST" => "Login",
                _ => "NotFound"
            };
        }
    }
}