using System;
using System.Threading.Tasks;
using BetterIrcForDotNet;

namespace Example
{
    class Program
    {
        static BetterIRC irc = new BetterIRC();
        static async Task Main(string[] args)
        {
            Random rand = new Random();
            irc.MessageRecieved += IrcOnMessageRecieved;
            irc.ConnectionStatus += IrcOnConnectionStatus;
            irc.IRC_Ready += IrcOnIRC_Ready;
            Task.Run(() =>
            {
                irc.ConnectToIRCAsync("irc.libera.chat", 8000, $"Random{rand.Next(0,1000)}", "");
            });
            //We can do things once it's ready.
            while (irc.ConnectedChannels.Count == 0)
            {
                //You should make sure you're connected to channels before doing anything with them.
                System.Threading.Thread.Sleep(500);
            }
            irc.SendMessage(irc.ConnectedChannels[0], "This is a message indicating this code ran!");
            irc.Disconnect("Boom"); //Doesn't seem to work presently.
            Console.ReadKey();
            
        }

        private static void IrcOnIRC_Ready(object? sender, bool e)
        {
            if (e == true)
            {
                //We need to join a channel for the bot to become usable beyond PM's.
                irc.ConnectToChannel("#krubot_test");
            }
            else
            {
                //We probably just DC'd.
                Console.WriteLine("No longer ready.");
            }

        }

        private static void IrcOnConnectionStatus(object? sender, bool e)
        {
            //THIS ONLY INDICATES IF WE HAVE A TCP CONNECTION
            //NOT IF WE CAN SEND MESSAGES!
            switch (e)
            {
                case true:
                    Console.WriteLine("Connected to the server");
                    break;
                case false:
                    Console.WriteLine("Disconnected from the Server");
                    break;
            }
        }

        private static void IrcOnMessageRecieved(object? sender, BetterIRC.ChatMessage e)
        {
            Console.WriteLine($"{e.Channel} - {e.Author}: {e.Message}");
            irc.SendMessage(e.Channel, $"Hello {e.Author}!");
        }
    }
}