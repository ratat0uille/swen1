using System.Net;
using MTCG.Server;

namespace MTCG
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await MTCG.Server.Server.StartAsync();
        }
    }
}