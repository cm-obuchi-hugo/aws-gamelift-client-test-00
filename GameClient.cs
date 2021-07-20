using System;

using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

using Amazon.GameLift;
using Amazon.GameLift.Model;


namespace AWSGameLiftClientTest
{
    class GameClient
    {
        public readonly System.Text.Encoding Encoder = System.Text.Encoding.UTF8;
        private TcpClient client = null;
        private NetworkStream stream = null;
        private string playerId = string.Empty;
        private AmazonGameLiftClient gameLiftClient = null;

        private GameSession gameSession = null;
        private PlayerSession playerSession = null;

        public bool IsAlive { get; private set; } = false;


        public GameClient()
        {
            IsAlive = true;

            playerId = Guid.NewGuid().ToString();
            gameLiftClient = new AmazonGameLiftClient("fake", "fake", new AmazonGameLiftConfig() { ServiceURL = "http://localhost:8080" });


        }

        public void Start()
        {
            CreateSessionsAndConnect();

        }

        async private Task CreateSessionsAndConnect()
        {
            await CreateGameSessionAsync();
            await CreatePlayerSessionAsync();
            Connect();
        }

        async private Task CreateGameSessionAsync()
        {
            Console.WriteLine($"Client : CreateGameSessionAsync() start");

            // GameSession gameSession = await CreateGameSessionAsync();
            var request = new CreateGameSessionRequest();
            request.FleetId = "fake";
            request.CreatorId = playerId;
            request.MaximumPlayerSessionCount = 1;

            Console.WriteLine($"Client : Sending request and await");
            var response = await gameLiftClient.CreateGameSessionAsync(request);
            Console.WriteLine($"Client : request sent");

            if (response.GameSession != null)
            {
                Console.WriteLine($"Client : GameSession Created!");
                Console.WriteLine($"Client : GameSession ID {response.GameSession.GameSessionId}!");
                gameSession = response.GameSession;
            }
            else
            {
                Console.Error.WriteLine($"Client : Failed creating GameSession!");
                IsAlive = false;
            }
        }

        async private Task CreatePlayerSessionAsync()
        {
            Console.WriteLine($"Client : CreatePlayerSessionAsync() start");
            if (gameSession == null) return;

            var request = new CreatePlayerSessionRequest();
            request.GameSessionId = gameSession.GameSessionId;
            request.PlayerId = playerId;

            Console.WriteLine($"Client : Sleep for a while");
            Thread.Sleep(10000);

            Console.WriteLine($"Client : Sending request and await");
            var response = await gameLiftClient.CreatePlayerSessionAsync(request);
            Console.WriteLine($"Client : request sent");

            if (response.PlayerSession != null)
            {
                Console.WriteLine($"Client : PlayerSession Created!");
                Console.WriteLine($"Client : PlayerSession ID {response.PlayerSession.PlayerSessionId}!");
                playerSession = response.PlayerSession;
            }
            else
            {
                Console.Error.WriteLine($"Client : Failed creating PlayerSession!");
                IsAlive = false;
            }
        }

        private void Connect()
        {
            Console.WriteLine($"Client : Connect() start");
            if (playerSession == null) return;

            Console.WriteLine($"Client : Try to connect");
            client = new TcpClient(playerSession.IpAddress, playerSession.Port);

            if (client.Connected)
            {
                Console.WriteLine($"Client : Connected");

                stream = client.GetStream();

                byte[] msg = new byte[256];

                Console.WriteLine($"Client : Wait to read from stream");
                while (stream.Read(msg) > 0)
                {
                    string str = Encoder.GetString(msg);
                    Console.WriteLine($"Received : {str}");

                    msg = Encoder.GetBytes("Hello Server, this is Client.");

                    stream.Write(msg);

                    Console.WriteLine($"Client : Message sent");

                    break;
                }

            }

            if (client != null) client.Close();

            IsAlive = false;
        }
    }
}