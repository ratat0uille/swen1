using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MTCG.Routing
{
    /*----------------------------------CLASS-HTTP-REQUEST-------------------------------------*/
    public class HttpRequest
    {
        public string Method { get; set; } 
        public string Path { get; set; }
        public string HttpVersion { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; }
    }

    /*----------------------------------CLASS-PARSER-------------------------------------*/
    public class Parser
    {
        public static async Task<HttpRequest> ParseAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                var request = new HttpRequest();

                /*----------------------------------REQUEST-LINE-------------------------------------*/
                var requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(requestLine)) throw new ArgumentException("Invalid HTTP request: missing request line.");

                var requestLineParts = requestLine.Split(' ');
                if (requestLineParts.Length != 3) throw new ArgumentException("Invalid HTTP request: malformed request line.");

                request.Method = requestLineParts[0];
                request.Path = requestLineParts[1];
                request.HttpVersion = requestLineParts[2];

                /*----------------------------------HEADERS-------------------------------------*/
                string line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                    var separatorIndex = line.IndexOf(": ");
                    if (separatorIndex == -1)
                        throw new ArgumentException($"Invalid HTTP header: {line}");

                    var headerName = line.Substring(0, separatorIndex);
                    var headerValue = line.Substring(separatorIndex + 2);
                    request.Headers[headerName] = headerValue;
                }

                /*----------------------------------BODY-------------------------------------*/
                if (request.Headers.TryGetValue("Content-Length", out var contentLengthValue) &&
                    int.TryParse(contentLengthValue, out var contentLength) && contentLength > 0)
                {
                    char[] buffer = new char[contentLength];
                    int totalRead = 0;
                    while (totalRead < contentLength)
                    {
                        int read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                        if (read == 0) throw new EndOfStreamException("Unexpected end of stream while reading body.");
                        totalRead += read;
                    }
                    request.Body = new string(buffer, 0, totalRead);
                }
                else
                {
                    request.Body = string.Empty;
                }

                return request;
            }
        }

        /*----------------------------------BODY-PARSER-METHOD-------------------------------------*/
        public static (string username, string password) BodyParser(Stream stream, HttpRequest request)
        {
            dynamic userInfo;

            userInfo = JsonConvert.DeserializeObject<dynamic>(request.Body);

            string username = userInfo.Username;
            string password = userInfo.Password;

            return (username, password);
        }
    }
}
