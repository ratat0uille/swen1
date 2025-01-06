using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MTCG.Routing
{
    public class Router
    {
        /*----------------------------------ROUTING-LOGIC-------------------------------------*/

        private readonly Dictionary<(string Path, string Method), string> _routes = new() //hier wird ne instanz einer dictionary
                                                                                          //erstellt, die "_routes" heißt
        {
            { ("/", "GET"), "Homepage" },
            { ("/register", "POST"), "Register" },      //key-value pairs ((path, method), route identifier) 
            { ("/login", "POST"), "Login" }
        };


        /*----------------------------------SUPPORTED-METHODS-------------------------------------*/

        private static readonly HashSet<string> SupportedMethods = new() { "GET", "POST", "PUT", "DELETE" }; //hier wird ne instanz eines hashsets
                                                                                                             //erstellt, das supported methods stored
                                                                                                             //hätte auch ne list oder array sein können,
                                                                                                             //aber allegedly ist n hashset effizienter? lol

        /*----------------------------------ROUTE-METHOD-------------------------------------*/

        public string Route(HttpRequest request) //nimmt n HttpRequest object als input um zu entscheiden wo es hin soll
        {
            if (request == null || string.IsNullOrEmpty(request.Method) //hier checken wir ob der http request eh vollständig ist
                                || string.IsNullOrEmpty(request.Path))
                return "BadRequest";                                   //und returnen badrequest falls dem so sei (hat sich
                                                                       //mittelalterlich angefühlt diesen satz zu schreiben lmao)

            if (!SupportedMethods.Contains(request.Method)) //hier das gleiche nur halt dass wir schauen obs ne method ist die wir zulassen
                return "MethodNotAllowed";

            return _routes.TryGetValue((request.Path, request.Method), out var result) ? result : "NotFound";   //hier schauen wir zuerst ob die gegebene combo
                                                                                                                //von path & method überhaupt in der _routes
                                                                                                                //dictionary existiert..
                                                                                                                //falls ja, dann wird der passende route identifier
                                                                                                                //returned, und wenn nicht, dann wird notfound returned
        }

        /*----------------------------------MIDDLEWARE-LOGIC-------------------------------------*/

        private readonly Dictionary<string, Func<HttpRequest, string>> _middleware = new()  //hier instantiaten wir ne middleware dictionary,
        {                                                                                   //die register und login pfade mit der authenticate
            { "/register", Authenticate },                                                  //method linkt (tbh middleware bisschen unnötig wenn ich noch nd mal ne db verbindung hab, aber whatever)
            { "/login", Authenticate }
        };                                                                               


        /*-----------------------AUTHENTICATE-METHOD-------------------------*/

        private static string Authenticate(HttpRequest request) //das ist basically um dann später (login) tokens zu authentifizieren
        {
            return request.Headers.ContainsKey("Authorization") ? "Authorized" : "Unauthorized";    //(shrimple if-else, returned entweder true oder "Unauthorized")
        }

        /*----------------------------DYNAMIC-ROUTE-MATCHING-METHOD------------------------------*/

        private static bool MatchDynamicPath(string path, string pattern)   //das ist dann für routes wie "user/{id}" wo die ID halt irgendwas sein kann
        {                                                                   //damit das alles nicht hardcoded werden muss weil das NIGHTMARISH wär    
            return Regex.IsMatch(path, pattern);
        }                               //und wenn hier "false" rauskommt, dann wirds zum default "NotFound"                                    

    }
}
