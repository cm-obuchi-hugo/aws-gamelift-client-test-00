using System;

namespace AWSGameLiftClientTest
{
    class Program
    {
        static private GameClient client = new GameClient();
        static void Main(string[] args)
        {
            client.Start();

            while(client.IsAlive)
            {

            }

            Console.WriteLine("Program ends.");
        }
    }
}
