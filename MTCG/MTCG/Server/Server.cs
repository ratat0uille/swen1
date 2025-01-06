using System;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HttpRequest = MTCG.Routing.HttpRequest;
using System.Collections.Generic;
using MTCG.Routing;
using MTCG.Models;

namespace MTCG.Server
{
    internal class Server
    {
        /*----------------------------------START-ASYNC-------------------------------------*/

        public static async Task StartAsync() //static = gehört zur class, braucht keine oject instanz
                                              //public = andere classes könnens callen wenn ich das richtig versteh
                                              //async = kann non-blocking tasks ausführen wenn man 'await' benutzt (d.h. das programm kann tasks gleichzeitig ausführen)
                                              //return type ist eine Task, d.h. ne laufende (oder abgeschlossene?) asynchrone operation
        {
            
            TcpListener listener = new TcpListener(IPAddress.Any, 10001); //kreirt ne instanz der TcpListener class
            listener.Start(); //startet listener der auf incoming connections listened
            Console.WriteLine("Server started on port 10001...");

            try
            {
                while (true) //infinite loop OBVIOUSLY aber ich schreibs trzd. dazu weil ich die einfachsten sachen vergesse LOL
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(); //akzeptiert connections asynchron;
                                                                              //blockiert während er wartet nicht den main thread,
                                                                              //d.h. er erlaubt das andere operations gleichzeitig weiter runnen
                   
                    _ = Task.Run(() => HandleClientAsync(client)); //hierhin wird jede eingehende client connection weitergeleitet,
                                                                   //was heißt, dass fürs handling von jeden client request ein
                                                                   //separater thread erstellt wird => der server kann concurrently
                                                                   //mehrere clients handlen
                                                                   
                }
            }
            catch (Exception exception) //catch block ist hier damit das programm nd crasht wenn was schiefgeht
                                        //(z.B. runtime errors oder invalid requests)
                                        //stattdessen wird eine exception wenn sie thrown wird hier gecatched
            {
                Console.WriteLine($"Server error: {exception.Message}"); //und es wird ne error message displayed
                                                                         //(rember das ist gut für debugging!!)
            }
            finally //der finally block runnt egal was passiert (exception or not)
                    //benutzen wir damit die server resourcen ordentlich upgecleaned werden
            {
                listener.Stop(); //z.B. assured er hier dass der listener stoppt
                                 //und der server chillig downshuttet & der port released wird
                Console.WriteLine("Server stopped.");
            }
        }



        /*--------------------------------HANDLE-CLIENT-ASYNC---------------------------------------*/

        private static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                //hier usen (haha, see what i did there?) wir 'using' statements, weil sie sicherstellen, dass
                //die 'Dispose' method einer resource automatisch gecalled wird wenn der code mit ihr done ist
                //das ist hier wichtig weils sonst zu memory leaks kommen kann (digga thank god dass es sowas gibt.... in C war das nightmarish)

                using (client) //using schließt hier die client connection wenn wir done sind
                using (NetworkStream stream = client.GetStream()) //hier closed & cleaned using den networkstream (benutzen wir fürs daten lesen/schreiben) wenn wir fertig sind
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true }) //hier ensured using dass der streamwriter
                                                                                                                     //(benutzen wir um daten in den stream zu schreiben)
                                                                                                                     //disposed und flushed wird; Autoflush benutzen wir,
                                                                                                                     //damit keine daten im buffer bleiben, also basically
                                                                                                                     //damit der client nicht unnötig lang auf ne response warten muss
                {
                    Routing.HttpRequest request = await Parser.ParseAsync(stream); //parsed den incoming http request
                                                                                   //& konvertiert ihn in ein strukturiertes
                                                                                   //HttpRequest object

                    Router router = new Router(); //macht ne neue instanz der router class, die dann bestimmt,
                                                  //welcher teil vom server mim request dealen soll

                    string routeResult = router.Route(request); //nimmt HttpRequest object und schaut basierend auf method & path
                                                                //was damit gemacht werden soll, bzw es returned einen string,
                                                                //der uns die route sagt
                    

                    switch (routeResult) //nimmt den string den wir grad bekommen haben und returned
                                         //basierend darauf ne response, die an den client geschickt wird
                    {
                        case "NotFound":
                            WriteResponse(NotFound(), writer);
                            break;
                        case "BadRequest":
                            WriteResponse(BadRequest(), writer);
                            break;
                        case "MethodNotAllowed":
                            WriteResponse(MethodNotAllowed(), writer);
                            break;
                        case "Unauthorized":
                            WriteResponse(Unauthorized(), writer);
                            break;
                        case "Login":
                            WriteResponse(Login(request, stream), writer); //callt die login method und übergibt
                                                                           //ihr den parsed request und stream;
                                                                           //login method handled login logic und
                                                                           //returned ne http response
                            break;
                        case "Register":
                            WriteResponse(Register(request, stream), writer); //das gleiche wie login, nur halt register
                            break;
                        default:
                            WriteResponse(NotFound(), writer);
                            break;
                    }

                }
            }
            catch (Exception exception) //dieser bro ist - wie immmer (afaik) - hier, damit falls irgendein bullshit
                                        //passiert, der server nicht aufeinmal crasht (und um errors zu loggen)
            {
                Console.WriteLine($"Error handling client: {exception.Message}"); 
            }
        }



/*****************************************************************************************************************************************************************************/
//hier ist ein BIG divider, weil ich login & register methods eigentlich lieber in ner separaten file hätte, aber das mach ich später falls noch Zeit ist



        /*----------------------------------LOGIN-METHOD-------------------------------------*/

        static string Login(HttpRequest request, Stream stream)
        {
            //200 OK
            //401 Unauthorized

            string response; //platzhalter; stored die generated http response
            var (username, password) = Parser.BodyParser(stream, request); //liest http req body, deserialized,
                                                                           //extrahiert username & passwort 

            string filePath = "../UserData/users.json"; //spezifiziert wo user data gestored wird, ist grad ne json file,
                                                        //aber das muss ich noch ändern (postgres)

            if (!File.Exists(filePath)) //checkt ab obs die file überhaupt gibt, und wenn nicht,
                                        //dann wird einf angenommen, dass die db nicht existiert lol
            {
                return GenerateResponse("404", "Not Found"); //obviously returnts dann n 404

            }

            var users = JsonConvert.DeserializeObject<List<Users>>(File.ReadAllText(filePath)) ?? new List<Users>();

            var user = users.Find(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                return GenerateResponse("200", "OK"); //wenns user gibt dann 200
            }
            else
            {
                return GenerateResponse("401", "Invalid username or password"); //sonst 401

            }

        } //aber digga eigentlich muss ich das für postgresql eh alles ändern, warum schau ich mir das jz an??



        /*----------------------------------REGISTER-METHOD-------------------------------------*/

        static string Register(HttpRequest request, Stream stream)
        {
            //201 created
            //409 User already exists

            var (username, password) = Parser.BodyParser(stream, request); //liest body mit Parser.BodyParser und extrahiert
                                                                           //username und passwort
            
            string filePath = "../UserData/users.json";
            
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]");
            }

            var users = JsonConvert.DeserializeObject<List<Users>>(File.ReadAllText(filePath)) ?? new List<Users>();

            if (users.Exists(user => user.Username == username))
            {

                return GenerateResponse("409", "User already exists");
            }

            users.Add(new Users{Username = username, Password = password});
            
            File.WriteAllText(filePath, JsonConvert.SerializeObject(users, Formatting.Indented));
            
            return GenerateResponse("201", "User created");
            
        }

/*****************************************************************************************************************************************************************************/




        /*--------------------------------------RESPONSE-STUFF----------------------------------------------*/

        //einf methods für responses



        /*--------------------------------GENERATE-RESPONSE---------------------------------------*/

        static string GenerateResponse(string status, string content) //status = http status code/reason
                                                                      //content = der body der http response
        {
            return $"HTTP/1.1 {status}\r\n" + 
                   "Content-Type: text/plain\r\n" +
                   $"Content-Length: {Encoding.UTF8.GetByteCount(content)}\r\n" +
                   "Connection: close\r\n" +
                   "\r\n" +
                   $"{content}";
        }

        /*----------------------------------WRITE-RESPONSE-------------------------------------*/

        static async void WriteResponse(string response, StreamWriter writer) //response = full response (von GenerateResponse generiert)
                                                                              //writer = mit dem clients networkstream verbundener streamwriter (used 2 send data 2 client)
        {
            Console.WriteLine(response); //für debugging
            await writer.WriteAsync(response); //schreibt asynchron den ganzen
                                               //response string in den stream vom client
                                               //(gut weil der server dann immer noch 2 other
                                               //requests responsive bleibt, auch während er schreibt)
                                               
            await writer.FlushAsync(); //kann sein dass nach dem writen immer noch data im internal buffer vom writer ist,
                                       //deshalb force flushen wir - nur so auf nummer sicher - alle daten die maybe
                                       //noch da sind in den networkstream
        }





        /*--------------------------------------ERROR-MESSAGES-------------------------------------------------*/

        //hab hier unten error messages als methoden gemacht, damit ich weniger repetitiven/redundanten code hab..
        //aber keine ahnung ob man das so macht lol



        /*----------------------------------400-BAD-REQUEST-METHOD------------------------------------*/

        static string BadRequest()
        {
            return GenerateResponse("400", "Bad Request");
        }

        /*----------------------------------404-NOT-FOUND-METHOD-------------------------------------*/

        static string NotFound()
        {
            return GenerateResponse("404", "Not Found");
        }

        /*----------------------------405-METHOD-NOT-ALLOWED-METHOD---------------------------------*/

        static string MethodNotAllowed()
        {
            return GenerateResponse("405", "Method Not Allowed");
        }

        /*-----------------------------------401-UNAUTHORIZED----------------------------------------*/

        static string Unauthorized()
        {
            return GenerateResponse("401", "Unauthorized");
        }

    }
}
