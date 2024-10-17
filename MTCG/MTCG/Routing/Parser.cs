using System.Text;

namespace MTCG.Routing
{

    public class HttpRequest
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string HttpVersion { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; }
    }

    public class Parser
    {
        public static HttpRequest Parse(string rawRequest)
        {
            using (var reader = new StringReader(rawRequest))
            {
                var request = new HttpRequest();

                //request line
                var requestLine = reader.ReadLine();
                if (string.IsNullOrEmpty(requestLine))
                    throw new ArgumentException("Invalid HTTP request: missing request line.");

                var requestLineParts = requestLine.Split(' ');
                if (requestLineParts.Length != 3)
                    throw new ArgumentException("Invalid HTTP request: malformed request line.");

                request.Method = requestLineParts[0];
                request.Path = requestLineParts[1];
                request.HttpVersion = requestLineParts[2];

                //headers
                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    var separatorIndex = line.IndexOf(": ");
                    if (separatorIndex == -1) throw new ArgumentException($"Invalid HTTP header: {line}");

                    var headerName = line.Substring(0, separatorIndex);
                    var headerValue = line.Substring(separatorIndex + 2);
                    request.Headers[headerName] = headerValue;
                }

                //body
                var bodyBuilder = new StringBuilder();
                while ((line = reader.ReadLine()) != null) bodyBuilder.AppendLine(line);
                request.Body = bodyBuilder.ToString().TrimEnd();

                return request;
            }
        }
    }
}