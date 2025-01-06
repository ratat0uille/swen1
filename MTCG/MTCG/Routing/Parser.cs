using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MTCG.Routing
{
    /*----------------------------------CLASS-HTTP-REQUEST-------------------------------------*/

    public class HttpRequest //repräsentiert einen http request... das würde ich eigentlich auch lieber in den Models folder moven...
    {
        public string Method { get; set; } 
        public string Path { get; set; }
        public string HttpVersion { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; }
    }


    /*----------------------------------CLASS-PARSER-------------------------------------*/

    public class Parser //macht aus einem raw http request ein strukturiertes HttpRequest object
    {
        public static async Task<HttpRequest> ParseAsync(Stream stream) //runnt asynchron und returned ne task
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true)) //initialisiert nen streamreader, um vom stream zu lesen,
                                                                                          //"leaveOpen: true" => stream bleibt offen, nachdem reader disposed wurde
            {
                var request = new HttpRequest();    //initialisiert leeres HttpRequest object


                /*----------------------------------REQUEST-LINE-------------------------------------*/

                //request line variable initialisieren; lesen
                var requestLine = await reader.ReadLineAsync();
                //schauen obs die request line eh gibt
                if (string.IsNullOrEmpty(requestLine)) throw new ArgumentException("Invalid HTTP request: missing request line.");

                //request line in 3 parts splitten (immer da wo n abstand ist)
                var requestLineParts = requestLine.Split(' ');
                //schauen obs eh 3 parts  sind
                if (requestLineParts.Length != 3) throw new ArgumentException("Invalid HTTP request: malformed request line.");

                //die values der parts verden dem HttpRequest objekt assigned
                request.Method = requestLineParts[0];
                request.Path = requestLineParts[1];
                request.HttpVersion = requestLineParts[2];


                /*----------------------------------HEADERS-------------------------------------*/

                string line; 
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync())) //loop: lines lesen bis eine null oder empty ist
                {
                    var separatorIndex = line.IndexOf(": "); //der separatorIndex hat den index vom ": " separator
                    if (separatorIndex == -1) //das wäre der fall, wenn ": " nicht gefunden wird
                        throw new ArgumentException($"Invalid HTTP header: {line}"); //und wenn das so ist, dann ist es kein valider http header
                                                                                     //(weil http header so formatiert sind: "key: value"

                    var headerName = line.Substring(0, separatorIndex); //extrahiert header name
                    var headerValue = line.Substring(separatorIndex + 2); //extrahiert header value (+2 um ": " zu skippen)
                    request.Headers[headerName] = headerValue; //tut header name und value in die header dictionary vom HttpRequest objekt
                }


                /*----------------------------------BODY-------------------------------------*/

                if (request.Headers.TryGetValue("Content-Length", out var contentLengthValue) &&
                    int.TryParse(contentLengthValue, out var contentLength) && contentLength > 0) //schaut ob der content-length header...
                                                                                                  //1) existiert,
                                                                                                  //2) ein valid integer,
                                                                                                  //3) größer als 0
                                                                                                  //ist
                {
                    char[] buffer = new char[contentLength]; //character buffer, in den der content reinkommt
                    int totalRead = 0; //zählt wv char aus dem stream gelesen wurden

                    while (totalRead < contentLength) //loopt bis der ganze body gelesen wurde
                    {
                        int read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                        if (read == 0) throw new EndOfStreamException("Unexpected end of stream while reading body.");
                        totalRead += read;
                    }
                    request.Body = new string(buffer, 0, totalRead); //macht aus buffer einen string und tut ihn
                                                                     //in die body property vom HttpRequest objekt
                }
                else
                {
                    request.Body = string.Empty; //sonst ist es halt empty lol
                }

                return request;
            }
        }


        /*----------------------------------BODY-PARSER-METHOD-------------------------------------*/

        public static (string username, string password) BodyParser(Stream stream, HttpRequest request) 
        {
            if (string.IsNullOrEmpty(request.Body)) //schaut ob request body null oder empty
                throw new ArgumentException("Request body is empty!!");

            dynamic userInfo = JsonConvert.DeserializeObject<dynamic>(request.Body);

            if (userInfo == null || userInfo.Username == null || userInfo.Password == null) //schaut ob info, username oder pw null
                throw new ArgumentException("Invalid request body format!!");

            string username = userInfo.Username;   
            string password = userInfo.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) //schaut ob username oder pw leer sind
                throw new ArgumentException("Username or password can't be empty!!");

            return (username, password);
        }
    }
}
