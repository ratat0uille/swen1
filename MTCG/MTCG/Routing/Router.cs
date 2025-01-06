
namespace MTCG.Routing
{
    public class Router 
    {
        public string Route(HttpRequest request) //nimmt n HttpRequest object als input um zu schauen wo es hin soll
        {
            //wenn irgendeine wichtige info fehlt dann returnts "BadRequest"
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