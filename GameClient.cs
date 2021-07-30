using System;

using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Amazon.CognitoIdentity;


namespace AWSGameLiftClientTest
{
    class GameClient
    {
        // A UTF-8 encoder to process byte[] <-> string conversion
        public readonly System.Text.Encoding Encoder = System.Text.Encoding.UTF8;

        // .NET Socket TCP client
        private TcpClient client = null;

        // NetworkStream(derives from Stream class) of a TCP client
        private NetworkStream stream = null;
        
        // A unique player id will be generated later
        private string playerId = string.Empty;

        // All the GameLift related things will be processed by this AmazonGameLiftClient
        private AmazonGameLiftClient gameLiftClient = null;

        // Sessions contain general information
        private GameSession gameSession = null;
        private PlayerSession playerSession = null;

        // A instance's status flag of this class 
        public bool IsAlive { get; private set; } = false;


        public GameClient()
        {
            IsAlive = true;

            playerId = Guid.NewGuid().ToString();

            Console.WriteLine($"Client : playerId {playerId}");

            // gameLiftClient = new AmazonGameLiftClient("fake", "fake", new AmazonGameLiftConfig() { ServiceURL = "http://localhost:8080" });
            
            CognitoAWSCredentials credentials = new CognitoAWSCredentials(
                "Your-Identity-Pool-ID", // Identity pool ID
                RegionEndpoint.APNortheast1 // Region
            );

            gameLiftClient = new AmazonGameLiftClient(credentials, RegionEndpoint.APNortheast1);
        }

        public void Start()
        {
            // Create GameSession(async) -> Create PlayerSession(async) -> Connect ()
            CreateSessionsAndConnect();
        }

        async private Task CreateSessionsAndConnect()
        {
            await CreateGameSessionAsync();
            await CreatePlayerSessionAsync();

            // Connect to the IP provided by PlayerSession
            Connect();
        }

        async private Task CreateGameSessionAsync()
        {
            Console.WriteLine($"Client : CreateGameSessionAsync() start");

            // GameSession gameSession = await CreateGameSessionAsync();
            var request = new CreateGameSessionRequest();
            request.FleetId = "fleet-id";
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


        // Connect to the IP which PlayerSession provides
        // When client connects : 
        // 1) Receive the msg sent by server
        // 2) Send another msg back
        // 3) Close the connection
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

                // A binary buffer for message
                byte[] msg = new byte[256];

                // Read the message (Stream.Read() blocks)
                Console.WriteLine($"Client : Wait to read from stream");
                while (stream.Read(msg) > 0)
                {
                    // Decode the binary msg to string
                    string str = Encoder.GetString(msg);
                    Console.WriteLine($"Received : {str}");

                    // Encode the string to binary message and send it
                    msg = Encoder.GetBytes("Hello Server, this is Client.");
                    stream.Write(msg);

                    Console.WriteLine($"Client : Message sent");

                    break;
                }

            }
            
            // After a successfule connection, close it
            client.Close();

            IsAlive = false;
        }
    }
}