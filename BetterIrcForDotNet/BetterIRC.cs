using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
//https://cerkit.com/2019/01/10/create-a-simple-irc-bot-with-net-core/
// ReSharper disable MethodHasAsyncOverload
//https://datatracker.ietf.org/doc/html/rfc2812

namespace BetterIrcForDotNet
{
    public class BetterIRC
    {
        private static class Controls
        {
            internal static TcpClient client = new TcpClient();
            internal static StreamWriter writer;
            internal static StreamReader reader;
        }

        public List<string> ConnectedChannels = new List<string>();
        public bool connected = false;
        public async Task ConnectToIRCAsync(string URL, int Port, string Username, string Password = "", bool verbose = false)
        {
            Controls.client.Connect(URL, Port);
            Controls.reader = new StreamReader(Controls.client.GetStream());
            Controls.writer = new StreamWriter(Controls.client.GetStream()) { NewLine = "\r\n", AutoFlush = true };;
            Controls.writer.WriteLineAsync($"USER {Username} * 8 BetterIRC");
            Controls.writer.WriteLineAsync($"NICK {Username}"); 
            Controls.writer.FlushAsync();
            RaiseConnectedMessage(Controls.client.Connected);
            while (Controls.client.Connected)
            {
                connected = Controls.client.Connected;
                var data = Controls.reader.ReadLine();
                if (data != null)
                {
                    var d = data.Split(' ');
                    if (verbose)
                    {
                        Console.WriteLine(data);
                    }
                    if (d[0] == "PING")
                    {
                        //Handle Ping
                        //Doing a replace means that any tokens the server attach stay intact.
                        Controls.writer.WriteLine(data.Replace("PING", "PONG"));
                    }

                    if (d.Length > 1)
                        switch (d[1])
                        {
                            case "376":
                                RaiseReady(true);
                                break;
                            case "001":
                                break;
                            case "PRIVMSG":
                                string p = d[0].Split('!')[0];
                                var msgbeta = data.Split(':');
                                var msg = msgbeta[^1];
                                RaiseChatMessage(p, d[2],msg);
                                break;
                            
                        }
                }
            }
            RaiseConnectedMessage(Controls.client.Connected);
            connected = Controls.client.Connected;
        }

        public void ConnectToChannel(string Channel)
        {
            string toConnectTo;
            if (!Channel.StartsWith("#"))
            {
                toConnectTo = $"#{Channel}";
            }
            else
            {
                toConnectTo = Channel;
            }
            ConnectedChannels.Add(toConnectTo);
            Controls.writer.WriteLine($"JOIN {toConnectTo}");
            Console.WriteLine($"Joined Channel {toConnectTo}");
        }

        public void Disconnect(string message)
        {
            Controls.writer.WriteLine($"QUIT {message}");
            RaiseReady(false);
            RaiseConnectedMessage(false);
            connected = false;
        }

        /// <summary>
        /// Raised when a Chat Message is recieved.
        /// </summary>
        /// <param name="user">Username of the Sender if applicable</param>
        /// <param name="channel">Channel the message was sent from</param>
        /// <param name="message">The message that was sent</param>
        private void RaiseChatMessage(string user, string channel, string message)
        {
            if (MessageRecieved != null)
            {
                //OverdrawnArgs args = new OverdrawnArgs();
                var args = new ChatMessage();
                args.Author = user.Replace(":", "");
                args.Author = "@" + args.Author;
                args.Message = message;
                args.Channel = channel;
                MessageRecieved(this, args);
            }
        }
        /// <summary>
        /// Tells the external application if we are connected or not.
        /// </summary>
        /// <param name="connected">Connection Status - True is Connected.</param>
        private void RaiseConnectedMessage(bool connected)
        {
            if (ConnectionStatus != null) ConnectionStatus(this, connected);
        }

        public void SendMessage(string target, string message)
        {
            if (target.StartsWith("#"))
            {
                if (ConnectedChannels.Contains(target))
                {
                    Controls.writer.WriteLine($"PRIVMSG {target} :{message}");
                }
            }
            else
            {
                //Must be a PM
                Controls.writer.WriteLine($"PRIVMSG {target} :{message}");
            }
        }
        private void RaiseReady(bool ready)
        {
            if (IRC_Ready != null) IRC_Ready(this, ready);
        }

        public event EventHandler<bool> IRC_Ready;
        public event EventHandler<bool> ConnectionStatus;
        
        public event EventHandler<ChatMessage> MessageRecieved;
        public class ChatMessage : EventArgs
        {
            public string Author { get; set; }
            public string Message { get; set; }
            public string Channel { get; set; }
        }
    }

}